// SPDX-License-Identifier: GPL-3.0-or-later
// The AGC state/link/draw sequence is adapted from SharpProspero Renderer3D.
// SharpProspero copyright (C) 2026 SvenGDK, licensed GPL-3.0-or-later.
// MM ENGINE changes: multi-draw ownership, diagnostics, validation, and failure reporting.
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using MMEngine.Core;
using SharpProspero.Graphics;
using SharpProspero.Graphics.Agc;
using SharpProspero.Interop;
using SharpProspero.Interop.Agc;
using SharpProspero.Interop.VideoOut;
using SharpProspero.Memory;

namespace MMEngine.Graphics3D;

/// <summary>Multi-draw AGC scene renderer derived from the verified SharpProspero 0.8 renderer path.</summary>
public sealed unsafe class MultiMeshRenderer3D : IDisposable
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Constants
    {
        public Matrix4x4 Mvp;
        public Matrix4x4 Model;
    }

    private const uint TargetMaskOffset = 0x008E;
    private const uint PrimitiveTriangleList = 4;
    private const byte Index32Bit = 1;
    private const int ShaderLinkageRegisterCount = 34;
    private const int PrimitiveStateRegisterCount = 3;
    private const uint VideoOutFlipModeVSync = 1;

    private readonly DisplayDevice _display;
    private readonly PreparedShader _vs;
    private readonly PreparedShader _ps;
    private readonly DrawCommandBuffer[] _commandBuffers;
    private readonly DirectMemoryRegion[] _contextState;
    private readonly DirectMemoryRegion[] _shaderState;
    private readonly DirectMemoryRegion[] _primitiveState;
    private readonly DirectMemoryRegion[] _constants;
    private readonly EngineDiagnostics? _diagnostics;
    private readonly int _maxContext;
    private readonly int _maxShader;
    private readonly int _framesInFlight;
    private readonly int _maxDrawsPerFrame;
    private int _slot;
    private int _drawCount;
    private bool _frameActive;
    private bool _disposed;

    public MultiMeshRenderer3D(DisplayDevice display, int commandBufferBytes = 512 * 1024,
        int maxDrawsPerFrame = 128, EngineDiagnostics? diagnostics = null)
    {
        ArgumentNullException.ThrowIfNull(display);
        ArgumentOutOfRangeException.ThrowIfLessThan(commandBufferBytes, 64 * 1024);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxDrawsPerFrame, 1);
        _display = display;
        _maxDrawsPerFrame = maxDrawsPerFrame;
        _diagnostics = diagnostics;
        AgcDevice.Initialize();
        _vs = BuiltInShaders.MeshVertex().Prepare();
        _ps = BuiltInShaders.MeshPixel().Prepare();

        _maxContext = CxRenderTarget.RegisterCount + AgcViewport.RegisterCount + 1 + ShaderLinkageRegisterCount
            + _vs.Shader.ContextRegisters.Length + _ps.Shader.ContextRegisters.Length;
        _maxShader = _vs.Shader.ShaderRegisters.Length + _ps.Shader.ShaderRegisters.Length;
        _framesInFlight = Math.Max(2, _display.BufferCount);
        _commandBuffers = new DrawCommandBuffer[_framesInFlight];
        _contextState = new DirectMemoryRegion[_framesInFlight];
        _shaderState = new DirectMemoryRegion[_framesInFlight];
        _primitiveState = new DirectMemoryRegion[_framesInFlight];
        _constants = new DirectMemoryRegion[_framesInFlight];
        for (int i = 0; i < _framesInFlight; i++)
        {
            _commandBuffers[i] = DrawCommandBuffer.Allocate((uint)commandBufferBytes);
            _contextState[i] = DirectMemoryRegion.Allocate((nuint)(_maxContext * sizeof(CxRegister)));
            _shaderState[i] = DirectMemoryRegion.Allocate((nuint)(_maxShader * sizeof(CxRegister)));
            _primitiveState[i] = DirectMemoryRegion.Allocate((nuint)(PrimitiveStateRegisterCount * sizeof(CxRegister)));
            _constants[i] = DirectMemoryRegion.Allocate((nuint)(_maxDrawsPerFrame * sizeof(Constants)));
        }
    }

    public bool IsFrameActive => _frameActive;
    public int DrawCount => _drawCount;

    public void BeginFrame()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_frameActive)
            throw new InvalidOperationException("A 3D frame is already active.");

        DrawCommandBuffer commandBuffer = _commandBuffers[_slot];
        DirectMemoryRegion contextRegion = _contextState[_slot];
        DirectMemoryRegion shaderRegion = _shaderState[_slot];
        DirectMemoryRegion primitiveRegion = _primitiveState[_slot];

        var target = new CxRenderTarget().Init(RegisterDefaults.RenderTargetBlock());
        var spec = new RenderTargetSpec(
            CxRenderTarget.Format.k8_8_8_8,
            CxRenderTarget.ChannelType.kUNorm,
            CxRenderTarget.ChannelOrder.kAlt,
            (uint)_display.Width,
            (uint)_display.Height,
            (ulong)_display.BackBufferAddress,
            _display.Tiling == VideoOutTilingMode.Tiled ? CxRenderTarget.TileMode.kRenderTarget : CxRenderTarget.TileMode.kLinear);
        AgcRenderTargetSetup.Initialize(target, spec);

        var viewport = new AgcViewport();
        viewport.SetViewport(0, 0, _display.Width, _display.Height);
        int cx = 0;
        CxRegister* contextBase = (CxRegister*)contextRegion.Pointer;
        var context = new Span<CxRegister>(contextRegion.Pointer, _maxContext);
        target.Registers.CopyTo(context[cx..]);
        cx += CxRenderTarget.RegisterCount;
        cx += viewport.WriteTo(context[cx..]);
        context[cx++] = new CxRegister((ushort)TargetMaskOffset, 0xF);

        void* linkage = contextBase + cx;
        cx += ShaderLinkageRegisterCount;
        SceResult.ThrowIfFailed(
            SceAgc.sceAgcLinkShaders(linkage, primitiveRegion.Pointer, null,
                _vs.Shader.Handle, _ps.Shader.Handle, PrimitiveTriangleList),
            nameof(SceAgc.sceAgcLinkShaders));
        _vs.Shader.ContextRegisters.CopyTo(context[cx..]);
        cx += _vs.Shader.ContextRegisters.Length;
        _ps.Shader.ContextRegisters.CopyTo(context[cx..]);
        cx += _ps.Shader.ContextRegisters.Length;

        int sh = 0;
        var shader = new Span<CxRegister>(shaderRegion.Pointer, _maxShader);
        _vs.Shader.ShaderRegisters.CopyTo(shader[sh..]);
        sh += _vs.Shader.ShaderRegisters.Length;
        _ps.Shader.ShaderRegisters.CopyTo(shader[sh..]);
        sh += _ps.Shader.ShaderRegisters.Length;

        commandBuffer.Reset();
        commandBuffer.WaitUntilSafeForDisplay(_display.OutputHandle, (uint)_display.CurrentBufferIndex);
        RequirePacket(SceAgc.sceAgcDcbSetCxRegistersIndirect(commandBuffer.Handle, contextRegion.Pointer, (uint)cx), "context state");
        RequirePacket(SceAgc.sceAgcDcbSetShRegistersIndirect(commandBuffer.Handle, shaderRegion.Pointer, (uint)sh), "shader state");
        RequirePacket(SceAgc.sceAgcDcbSetUcRegistersIndirect(commandBuffer.Handle, primitiveRegion.Pointer, PrimitiveStateRegisterCount), "primitive state");
        RequirePacket((void*)commandBuffer.SetIndexSize(Index32Bit), "index size");
        _drawCount = 0;
        _frameActive = true;
    }

    public void Draw(GpuMesh3D mesh, in Matrix4x4 model, in Matrix4x4 viewProjection)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        for (int i = 0; i < mesh.SubMeshBuffers.Length; i++)
            DrawSubMesh(mesh.SubMeshBuffers[i], model * viewProjection, model);
    }

    public void DrawSubMesh(MeshBuffer mesh, in Matrix4x4 mvp, in Matrix4x4 model)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_frameActive)
            throw new InvalidOperationException("Call BeginFrame before drawing.");
        if (_drawCount >= _maxDrawsPerFrame)
            throw new InvalidOperationException($"3D renderer exceeded its {_maxDrawsPerFrame} draws-per-frame reservation.");

        DrawCommandBuffer commandBuffer = _commandBuffers[_slot];
        DirectMemoryRegion constantsRegion = _constants[_slot];
        int constantsOffset = _drawCount * sizeof(Constants);
        Constants* constants = (Constants*)((byte*)constantsRegion.Pointer + constantsOffset);
        constants->Mvp = mvp;
        constants->Model = model;

        AgcBufferDescriptor cb = AgcBufferDescriptor.Constant(
            (ulong)((byte*)constantsRegion.Pointer + constantsOffset), (uint)sizeof(Constants));
        AgcBufferDescriptor vb = AgcBufferDescriptor.Structured(
            (ulong)mesh.VertexAddress, (uint)MeshBuffer.VertexStride, (uint)mesh.VertexCount);
        BindVertexResource(commandBuffer.Handle, ShaderResourceKind.ConstantBuffer, cb);
        BindVertexResource(commandBuffer.Handle, ShaderResourceKind.ReadOnly, vb);
        RequirePacket((void*)commandBuffer.SetIndexBuffer(mesh.IndexAddress), "index buffer");
        RequirePacket((void*)commandBuffer.SetIndexCount((uint)mesh.IndexCount), "index count");
        RequirePacket((void*)commandBuffer.DrawIndex((uint)mesh.IndexCount, mesh.IndexAddress), "indexed draw");
        _drawCount++;
        if (_diagnostics is not null)
        {
            _diagnostics.DrawCalls3D++;
            _diagnostics.Triangles += mesh.IndexCount / 3;
        }
    }

    public void EndFrame()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_frameActive)
            throw new InvalidOperationException("No 3D frame is active.");
        DrawCommandBuffer commandBuffer = _commandBuffers[_slot];
        RequirePacket(SceAgc.sceAgcDcbSetFlip(commandBuffer.Handle, (uint)_display.OutputHandle,
            _display.CurrentBufferIndex, VideoOutFlipModeVSync, (long)_display.FrameIndex), "display flip");
        AgcDevice.Submit(commandBuffer);
        int suspendResult = AgcDevice.SuspendPoint();
        SceResult.ThrowIfFailed(suspendResult, nameof(AgcDevice.SuspendPoint));
        _display.AdvanceFrame();
        _slot = (_slot + 1) % _framesInFlight;
        _frameActive = false;
    }

    private void BindVertexResource(void* commandBuffer, ShaderResourceKind kind, in AgcBufferDescriptor descriptor)
    {
        if (!_vs.Shader.TryGetResourceSlot(kind, 0, out int dwordOffset, out _))
            throw new InvalidOperationException($"Built-in vertex shader does not expose required resource {kind}[0].");
        uint* words = stackalloc uint[4];
        descriptor.WriteTo(new Span<uint>(words, 4));
        RequirePacket(SceAgc.sceAgcCbSetShRegisterRangeDirect(commandBuffer,
            AgcShader.GsUserDataBaseOffset + (uint)dwordOffset, words, 4), $"vertex resource {kind}");
    }

    private static void RequirePacket(void* packet, string operation)
    {
        if (packet is null)
            throw new InvalidOperationException($"AGC command buffer ran out of room while recording {operation}.");
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _frameActive = false;
        _disposed = true;
        _vs.Dispose();
        _ps.Dispose();
        for (int i = 0; i < _framesInFlight; i++)
        {
            _commandBuffers[i].Dispose();
            _contextState[i].Dispose();
            _shaderState[i].Dispose();
            _primitiveState[i].Dispose();
            _constants[i].Dispose();
        }
    }
}
