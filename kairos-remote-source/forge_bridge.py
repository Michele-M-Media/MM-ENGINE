"""Private, owner-controlled MCP relay for one already-authorized desktop.

Runs in the *same* HTTPS deployment as the public AITMPL read-only gateway,
but on separate /private/mcp and /worker/* paths. No shell is reachable from
MCP. Owner and worker use distinct long bearer credentials; these values are
never checked into source control. Worker operates only on LOCAL allowlisted
workspace paths, without an inbound port.

The free Render filesystem is ephemeral. Queued jobs are not guaranteed to
survive a server replacement. Do not use as an unattended durable task queue.
"""
from __future__ import annotations
import asyncio
import hmac
import json
import os
from pathlib import Path, PurePosixPath
import secrets
import threading
import time
from uuid import uuid4
from starlette.requests import Request
from starlette.responses import JSONResponse, PlainTextResponse, Response
from starlette.routing import Route

ALLOWED = frozenset({'tree', 'read', 'search', 'git_status', 'checks'})
PROTOCOLS = ('2025-03-26','2025-06-18','2025-11-25','2026-07-28')
CLIENT_MCP_VERSION = '2025-11-25'
MAX_BODY=16000
MAX_RESULT=24000
LEASE_SECONDS=120
STATE_DIR=Path(os.getenv('KAIROS_FORGE_QUEUE_DIR','/tmp/kairos-forge-remote-queue'))
WORKER_ID=os.getenv('KAIROS_WORKER_ID','desktop-primary')
WORKSPACE_ID=os.getenv('KAIROS_WORKSPACE_ID','kairos-demo')
_LOCK=threading.RLock()
_HEARTBEAT=0.0


def _secrets_ready():
    owner=os.getenv('KAIROS_OWNER_TOKEN','')
    worker=os.getenv('KAIROS_WORKER_TOKEN','')
    return len(owner)>=48 and len(worker)>=48 and not hmac.compare_digest(owner,worker)


def _auth(request:Request,worker:bool=False):
    if not _secrets_ready():return False
    secret=os.getenv('KAIROS_WORKER_TOKEN' if worker else 'KAIROS_OWNER_TOKEN','')
    value=request.headers.get('authorization','')
    return hmac.compare_digest(value,'Bearer '+secret)


def _unauthorized():
    return PlainTextResponse('Unauthorized',status_code=401,headers={'Cache-Control':'no-store','WWW-Authenticate':'Bearer realm="kairos-forge-private"'})


async def _json(request:Request):
    if request.headers.get('content-type','').split(';')[0].strip().lower() not in ('application/json',''):
        raise ValueError('Expected application/json')
    chunks=[];count=0
    async for part in request.stream():
        count+=len(part)
        if count>MAX_BODY:raise ValueError('Payload too large')
        chunks.append(part)
    try:data=json.loads(b''.join(chunks))
    except (UnicodeDecodeError,json.JSONDecodeError) as exc:raise ValueError('Malformed JSON') from exc
    if not isinstance(data,dict):raise ValueError('JSON object expected')
    return data


def _path(job_id:str):
    if len(job_id)!=32 or any(ch not in '0123456789abcdef' for ch in job_id):
        raise ValueError('Invalid job id')
    return STATE_DIR/(job_id+'.json')


def _save(doc):
    STATE_DIR.mkdir(parents=True,exist_ok=True)
    dst=_path(doc['id']);tmp=dst.with_suffix('.tmp.'+secrets.token_hex(4))
    tmp.write_text(json.dumps(doc,sort_keys=True),encoding='utf-8')
    os.replace(tmp,dst)


def _load(job_id):
    try:return json.loads(_path(job_id).read_text(encoding='utf-8'))
    except FileNotFoundError:raise ValueError('Job not found') from None


def _relative(text):
    if not isinstance(text,str) or len(text)>180 or '\\' in text or '\x00' in text or ':' in text:
        raise ValueError('Unsafe relative path')
    p=PurePosixPath(text)
    if p.is_absolute() or '..' in p.parts or any(x.startswith('.') for x in p.parts if x not in ('.',)):
        raise ValueError('Unsafe relative path')
    return str(p)


def queue_job(args):
    if not isinstance(args,dict):raise ValueError('Arguments required')
    if args.get('workspace')!=WORKSPACE_ID:raise PermissionError('Workspace not enabled')
    op=args.get('operation')
    if op not in ALLOWED:raise ValueError('Operation not permitted')
    rel=_relative(args.get('path',''))
    query=args.get('query','')
    if op=='read' and not rel:raise ValueError('read requires path')
    if op=='search' and (not isinstance(query,str) or not 2<=len(query)<=100):
        raise ValueError('search requires query length 2..100')
    if op!='search':query=''
    doc={'id':uuid4().hex,'worker':WORKER_ID,'workspace':WORKSPACE_ID,'operation':op,
         'path':rel,'query':query,'state':'queued','created':time.time(),'attempts':0,
         'lease_until':0.0,'lease_token':None,'result':None}
    with _LOCK:_save(doc)
    return {'job_id':doc['id'],'state':'queued','workspace':WORKSPACE_ID,'operation':op}


def job_result(args):
    doc=_load(args.get('job_id',''))
    return {key:doc.get(key) for key in ('id','workspace','operation','state','attempts','result')}


def cancel(args):
    with _LOCK:
        d=_load(args.get('job_id',''))
        if d['state'] not in ('queued',):return {'job_id':d['id'],'state':d['state'],'cancelled':False}
        d['state']='cancelled';_save(d)
    return {'job_id':d['id'],'state':'cancelled','cancelled':True}


def claim(worker_id):
    global _HEARTBEAT
    if worker_id!=WORKER_ID:raise PermissionError('Unknown worker')
    with _LOCK:
        _HEARTBEAT=time.time()
        STATE_DIR.mkdir(parents=True,exist_ok=True)
        for name in sorted(STATE_DIR.glob('*.json'),key=lambda x:x.stat().st_mtime):
            try:d=json.loads(name.read_text(encoding='utf-8'))
            except (ValueError,OSError):continue
            if d.get('worker')!=WORKER_ID:continue
            if d['state']=='leased' and d['lease_until']<time.time():
                if d['attempts']>=2:d['state']='interrupted';_save(d)
                else:d['state']='queued';_save(d)
            if d['state']!='queued':continue
            d['state']='leased';d['attempts']+=1
            d['lease_until']=time.time()+LEASE_SECONDS;d['lease_token']=secrets.token_urlsafe(24)
            _save(d)
            return {k:d[k] for k in ('id','workspace','operation','path','query','lease_token')}
    return None


def complete(data):
    if data.get('worker')!=WORKER_ID:raise PermissionError('Unknown worker')
    result=data.get('result')
    if not isinstance(result,dict) or len(json.dumps(result,ensure_ascii=False))>MAX_RESULT:
        raise ValueError('Oversize or invalid result')
    with _LOCK:
        d=_load(data.get('job_id',''))
        if d['worker']!=WORKER_ID or d['state']!='leased' or d['lease_until']<time.time() or not hmac.compare_digest(str(d['lease_token']),str(data.get('lease_token',''))):
            raise PermissionError('Invalid job lease')
        d['state']='completed';d['lease_token']=None;d['result']=result;_save(d)
    return {'job_id':d['id'],'state':'completed'}


def tools():
    def schema(props=None,required=()):return {'type':'object','properties':props or {},'required':list(required),'additionalProperties':False}
    ss=lambda description:{'type':'string','description':description}
    return [
        {'name':'kairos_private_status','description':'Show the authenticated Kairos Forge remote service and desktop worker availability; no secrets.','inputSchema':schema(), 'annotations':{'readOnlyHint':True}},
        {'name':'kairos_remote_queue','description':'Queue one allowlisted read/test request to an authorized desktop worker. Not an arbitrary shell.','inputSchema':schema({'workspace':ss('Allowed workspace alias'),'operation':{'type':'string','enum':sorted(ALLOWED)},'path':ss('Relative file path only for read'),'query':ss('Search text only for search')},('workspace','operation')), 'annotations':{'readOnlyHint':False}},
        {'name':'kairos_remote_result','description':'Get status or result of a remote desktop job.','inputSchema':schema({'job_id':ss('Job id')},('job_id',)),'annotations':{'readOnlyHint':True}},
        {'name':'kairos_remote_cancel','description':'Cancel one queued job before a worker claims it.','inputSchema':schema({'job_id':ss('Job id')},('job_id',)),'annotations':{'readOnlyHint':False}},
    ]


async def private_mcp(request:Request):
    if not _auth(request):return _unauthorized()
    try:body=await _json(request)
    except ValueError:return PlainTextResponse('Malformed request',status_code=400)
    rid=body.get('id');method=body.get('method')
    if method=='notifications/initialized':return Response(status_code=202)
    def reply(value=None,error=None):
        envelope={'jsonrpc':'2.0','id':rid}
        if error:envelope['error']=error
        else:envelope['result']=value
        return JSONResponse(envelope,headers={'Cache-Control':'no-store'})
    if method=='initialize':
        return reply({'protocolVersion':CLIENT_MCP_VERSION,'serverInfo':{'name':'kairos-forge-remote-external','version':'1.0.0'},
                      'capabilities':{'tools':{'listChanged':False}},'instructions':'External, owner-authorized Kairos MCP. Read-only desktop operations except preconfigured tests. No arbitrary shell, no paid AI APIs.'})
    if method=='ping':return reply({})
    if method=='tools/list':return reply({'tools':tools()})
    if method=='tools/call':
        params=body.get('params')
        if not isinstance(params,dict):return reply(error={'code':-32602,'message':'Invalid params'})
        name=params.get('name');args=params.get('arguments',{})
        if not isinstance(args,dict):return reply(error={'code':-32602,'message':'Invalid arguments'})
        try:
            if name=='kairos_private_status':
                data={'server':'kairos-forge-remote-external','version':'1.0.0','authenticated':True,
                      'worker_id':WORKER_ID,'worker_online':time.time()-_HEARTBEAT<35,
                      'workspaces':[WORKSPACE_ID],'allowed_operations':sorted(ALLOWED),
                      'paid_api_calls':False,'writes_enabled':False,'queue_persistent_across_restart':False}
            elif name=='kairos_remote_queue':data=queue_job(args)
            elif name=='kairos_remote_result':data=job_result(args)
            elif name=='kairos_remote_cancel':data=cancel(args)
            else:return reply(error={'code':-32601,'message':'Unknown tool'})
            return reply({'content':[{'type':'text','text':json.dumps(data,ensure_ascii=False)}],'isError':False})
        except (ValueError,KeyError,PermissionError,OSError,TypeError) as exc:
            return reply({'content':[{'type':'text','text':f'{type(exc).__name__}: {str(exc)[:160]}'}],'isError':True})
    return reply(error={'code':-32601,'message':'Unknown method'})


async def worker_endpoint(request:Request):
    if not _auth(request,worker=True):return _unauthorized()
    try:
        args=await _json(request)
        if args.get('worker')!=WORKER_ID:raise PermissionError('Unknown worker')
        if request.url.path=='/worker/claim':
            return JSONResponse({'job':claim(args['worker'])},headers={'Cache-Control':'no-store'})
        if request.url.path=='/worker/complete':
            return JSONResponse(complete(args),headers={'Cache-Control':'no-store'})
        raise ValueError('Unknown worker path')
    except (ValueError,KeyError,PermissionError,OSError,TypeError):
        return PlainTextResponse('Worker request rejected',status_code=400)


async def private_health(request:Request):
    if not _auth(request):return _unauthorized()
    return JSONResponse({'status':'ready' if _secrets_ready() else 'disabled','worker_online':time.time()-_HEARTBEAT<35,'no_api_billing':True},headers={'Cache-Control':'no-store'})


def routes():
    return [Route('/private/mcp',private_mcp,methods=['POST']),Route('/private/health',private_health,methods=['GET']),
            Route('/worker/claim',worker_endpoint,methods=['POST']),Route('/worker/complete',worker_endpoint,methods=['POST'])]
