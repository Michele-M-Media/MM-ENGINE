using System;
using MMEngine.Core;

namespace MMEngine.Scene;

/// <summary>Coordinates timing, diagnostics, services, and the active Scene.</summary>
public sealed class EngineRuntime : IDisposable
{
    private bool _disposed;

    public EngineClock Clock { get; } = new();
    public EngineDiagnostics Diagnostics { get; } = new();
    public ServiceRegistry Services { get; } = new();
    public SceneManager Scenes { get; } = new();

    public FrameTime Tick(double deltaSeconds)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        FrameTime time = Clock.Advance(deltaSeconds);
        Diagnostics.BeginFrame(time.FrameIndex);
        Scenes.Update(time, Diagnostics);
        return time;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        Scenes.Dispose();
        Services.Clear();
    }
}
