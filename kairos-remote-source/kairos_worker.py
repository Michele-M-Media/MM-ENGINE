"""Kairos external remote PC worker — Python 3.11+ stdlib only.

Run from the user's own authorized PC, without installation or Windows service.
Makes outbound HTTPS requests to an owner's gateway; no inbound TCP listener.
Workspace roots and approved checks exist only in the local private config.
Commands received from MCP can NEVER select arbitrary subprocess arguments.
"""
from __future__ import annotations
import argparse
import json
import os
from pathlib import Path
import subprocess
import time
from urllib.request import Request,build_opener,HTTPRedirectHandler
from urllib.error import HTTPError,URLError
from urllib.parse import urlsplit

ALLOWED={'tree','read','search','git_status','checks'}
MAX_FILE=500000
MAX_OUTPUT=18000
EXTENSIONS={'.py','.js','.ts','.json','.toml','.xml','.md','.txt','.c','.h','.cpp','.hpp','.cs','.rs','.yaml','.yml','.sh','.ps1'}


class BlockRedirect(HTTPRedirectHandler):
    def redirect_request(self,req,fp,code,msg,headers,newurl):
        raise ValueError('HTTP redirect prohibited for authenticated worker')


def load_config(path:Path):
    obj=json.loads(path.read_text(encoding='utf-8'))
    url=str(obj.get('gateway','')).rstrip('/')
    u=urlsplit(url)
    if u.scheme!='https' or not u.netloc or u.username or u.password or u.query or u.fragment or u.path:
        raise ValueError('gateway must be an HTTPS origin')
    if not isinstance(obj.get('worker_token'),str) or len(obj['worker_token'])<48:raise ValueError('missing worker token')
    if not isinstance(obj.get('worker_id'),str) or not obj['worker_id']:raise ValueError('missing worker id')
    ws=obj.get('workspaces')
    if not isinstance(ws,dict) or not ws:raise ValueError('no workspaces allowed')
    for alias,root in ws.items():
        if not isinstance(alias,str) or not isinstance(root,str) or not Path(root).resolve().is_dir():
            raise ValueError('invalid local workspace')
    return obj


def post(cfg,endpoint,data):
    payload=json.dumps(data,ensure_ascii=False).encode('utf-8')
    req=Request(cfg['gateway']+endpoint,data=payload,headers={
        'Authorization':'Bearer '+cfg['worker_token'],'Content-Type':'application/json','Accept':'application/json',
        'User-Agent':'KairosForgeDesktop/1.0'},method='POST')
    with build_opener(BlockRedirect()).open(req,timeout=18) as response:
        result=response.read(30000)
        return json.loads(result.decode('utf-8'))


def safe_path(root:Path,path:str):
    if not isinstance(path,str) or len(path)>180 or '\\' in path or '\x00' in path or ':' in path:raise ValueError('invalid relative path')
    from pathlib import PurePosixPath
    p=PurePosixPath(path)
    if p.is_absolute() or '..' in p.parts or any(x.startswith('.') for x in p.parts if x!='.'):
        raise ValueError('invalid relative path')
    base=root.resolve()
    target=(base/Path(*p.parts)).resolve()
    if not target.is_relative_to(base):raise PermissionError('outside approved workspace')
    if target.is_symlink():raise PermissionError('symlinks not permitted')
    return target


def collect_files(root:Path):
    count=0
    for base,dirs,files in os.walk(root,followlinks=False):
        dirs[:]=sorted([x for x in dirs if not x.startswith('.') and not (Path(base)/x).is_symlink()])
        if Path(base).relative_to(root).parts and len(Path(base).relative_to(root).parts)>=5:dirs[:]=[]
        for name in sorted(files):
            if name.startswith('.'):continue
            f=Path(base)/name
            if f.is_symlink() or not f.is_file():continue
            count+=1
            if count>400:return
            yield f


def do_job(cfg,job):
    alias=job.get('workspace')
    if alias not in cfg['workspaces']:
        return {'ok':False,'error':'workspace not allowed'}
    root=Path(cfg['workspaces'][alias]).resolve()
    op=job.get('operation')
    if op not in ALLOWED:return {'ok':False,'error':'operation not permitted'}
    try:
        if op=='tree':
            entries=[str(p.relative_to(root)).replace('\\','/') for p in collect_files(root)]
            return {'ok':True,'files':entries[:120],'truncated':len(entries)>120}
        if op=='read':
            file=safe_path(root,job.get('path',''))
            if not file.is_file() or file.stat().st_size>MAX_FILE:raise ValueError('file unavailable or too large')
            text=file.read_text(encoding='utf-8')
            return {'ok':True,'path':str(file.relative_to(root)).replace('\\','/'),'content':text[:MAX_OUTPUT],
                    'truncated':len(text)>MAX_OUTPUT}
        if op=='search':
            query=job.get('query','')
            if not isinstance(query,str) or not 2<=len(query)<=100:raise ValueError('invalid query')
            found=[];scanned=0
            for file in collect_files(root):
                if file.suffix.lower() not in EXTENSIONS or file.stat().st_size>MAX_FILE:continue
                scanned+=1
                try:lines=file.read_text(encoding='utf-8').splitlines()
                except (OSError,UnicodeError):continue
                for i,line in enumerate(lines,1):
                    if query.casefold() in line.casefold():
                        found.append({'file':str(file.relative_to(root)).replace('\\','/'),'line':i,'text':line[:200]})
                        if len(found)>=35:break
                if len(found)>=35:break
            return {'ok':True,'matches':found,'scanned_files':scanned,'truncated':len(found)>=35}
        if op=='git_status':
            proc=subprocess.run(['git','-C',str(root),'status','--short','--branch'],shell=False,
                                text=True,capture_output=True,encoding='utf-8',errors='replace',timeout=12)
            return {'ok':proc.returncode==0,'exit_code':proc.returncode,'stdout':proc.stdout[:9000],
                    'stderr':proc.stderr[:2000]}
        if op=='checks':
            checks=cfg.get('checks',{}).get(alias,[])
            if not isinstance(checks,list) or len(checks)>8 or not checks:raise ValueError('no locally approved checks')
            results=[]
            for args in checks:
                if not isinstance(args,list) or not args or not all(isinstance(x,str) and len(x)<=120 for x in args):
                    raise ValueError('invalid locally approved check')
                cp=subprocess.run(args,cwd=root,shell=False,encoding='utf-8',errors='replace',
                                  text=True,capture_output=True,timeout=45)
                results.append({'check':args,'exit_code':cp.returncode,'stdout':cp.stdout[:1500], 'stderr':cp.stderr[:1500]})
                if cp.returncode:break
            return {'ok':all(x['exit_code']==0 for x in results),'checks':results}
    except (OSError,UnicodeError,ValueError,PermissionError,subprocess.TimeoutExpired) as exc:
        return {'ok':False,'error':f'{type(exc).__name__}: {str(exc)[:180]}'}
    return {'ok':False,'error':'unsupported'}


def loop(cfg,once=False):
    print('Kairos worker connected to',cfg['gateway'],'id:',cfg['worker_id'], 'workspaces:',list(cfg['workspaces']),flush=True)
    while True:
        try:
            item=post(cfg,'/worker/claim',{'worker':cfg['worker_id']}).get('job')
            if item:
                result=do_job(cfg,item)
                wire=json.dumps(result,ensure_ascii=False)
                if len(wire)>22000:result={'ok':False,'error':'result limit exceeded'}
                post(cfg,'/worker/complete',{'worker':cfg['worker_id'],'job_id':item['id'],
                                            'lease_token':item['lease_token'],'result':result})
                print('job finished',item['id'],item['operation'],result.get('ok'),flush=True)
            if once:return
            time.sleep(5 if not item else 1)
        except (HTTPError,URLError,TimeoutError,OSError,ValueError) as exc:
            print('Gateway temporarily unavailable:',type(exc).__name__,flush=True)
            if once:raise
            time.sleep(12)


def self_test(cfg):
    alias=next(iter(cfg['workspaces']))
    root=Path(cfg['workspaces'][alias]).resolve()
    assert do_job(cfg,{'workspace':alias,'operation':'tree'})['ok']
    assert not do_job(cfg,{'workspace':alias,'operation':'read','path':'../private.txt'})['ok']
    assert not do_job(cfg,{'workspace':alias,'operation':'read','path':'C:/Windows/win.ini'})['ok']
    assert not do_job(cfg,{'workspace':alias,'operation':'unknown'})['ok']
    print('Kairos worker self-test PASS; root:',str(root),flush=True)


if __name__=='__main__':
    ap=argparse.ArgumentParser();ap.add_argument('--config',default='worker-config.private.json')
    ap.add_argument('--once',action='store_true');ap.add_argument('--self-test',action='store_true')
    args=ap.parse_args();config=load_config(Path(args.config))
    if args.self_test:self_test(config)
    else:loop(config,args.once)
