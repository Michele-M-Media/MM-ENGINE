using System;
using System.Diagnostics;
using MMEngine.Core;
using MMEngine.Graphics3D;
using MMEngine.Input;
using MMEngine.Scene;
using SharpProspero.Application;
using SharpProspero.Graphics;
using SharpProspero.Input;
using SharpProspero.Interop;
using SharpProspero.Interop.SystemService;
using SharpProspero.Interop.UserService;

namespace MMEngine.Platform.PS5;

/// <summary>GPU-owned frame loop; it avoids the CPU Present that would otherwise double-flip an AGC frame.</summary>
public abstract class MmApplication3D(AppConfig? config = null) : IDisposable
{
    private DisplayDevice? _display;
    private GamePad? _gamePad;
    private bool _exitRequested;
    private bool _disposed;
    private long _loopFrameIndex;
    private long _nextGamePadAttempt;

    private const long GamePadRetryFrames = 60;

    public AppConfig Config { get; } = config ?? new AppConfig();
    public EngineRuntime Engine { get; } = new();
    public DualSenseTracker Input { get; } = new();
    protected DisplayDevice Display => _display ?? throw new InvalidOperationException("Display is not open.");
    protected MultiMeshRenderer3D Renderer { get; private set; } = null!;

    public void Run()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Ps5Logging.UseDevelopmentConsole();
        InitializeServices();
        _display = DisplayDevice.Open(Config.Width, Config.Height, Config.BufferCount, SceUser.System);
        if (Config.OpenGamePad)
            TryOpenGamePad();
        Renderer = new MultiMeshRenderer3D(Display, diagnostics: Engine.Diagnostics);
        OnEngineLoad();
        try
        {
            long previous = Stopwatch.GetTimestamp();
            double frequency = Stopwatch.Frequency;
            while (!_exitRequested)
            {
                long now = Stopwatch.GetTimestamp();
                double deltaSeconds = (now - previous) / frequency;
                previous = now;
                if (_gamePad is null && Config.OpenGamePad && _loopFrameIndex >= _nextGamePadAttempt)
                    TryOpenGamePad();
                GamePadState padState = _gamePad?.Read() ?? GamePadState.Neutral;
                DualSenseSnapshot snapshot = Ps5Input.Convert(padState);
                Input.Update(snapshot);
                FrameTime time = Engine.Tick(deltaSeconds);
                OnEngineFrame(Renderer, time);
                if (Renderer.IsFrameActive)
                    throw new InvalidOperationException("OnEngineFrame returned without EndFrame.");
                _loopFrameIndex++;
            }
        }
        finally
        {
            OnEngineUnload();
        }
    }

    protected void RequestExit() => _exitRequested = true;
    protected bool SetControllerVibration(byte largeMotor, byte smallMotor)
        => _gamePad?.SetVibration(largeMotor, smallMotor) ?? false;
    protected bool SetControllerLightBar(byte red, byte green, byte blue)
        => _gamePad?.SetLightBar(red, green, blue) ?? false;
    protected virtual void OnEngineLoad() { }
    protected abstract void OnEngineFrame(MultiMeshRenderer3D renderer, in FrameTime time);
    protected virtual void OnEngineUnload() { }

    private unsafe void InitializeServices()
    {
        int priority = 700;
        UserService.sceUserServiceInitialize(&priority);
        if (Config.HideSplashScreen)
            SystemService.sceSystemServiceHideSplashScreen();
    }

    private void TryOpenGamePad()
    {
        _nextGamePadAttempt = _loopFrameIndex + GamePadRetryFrames;
        try
        {
            _gamePad = GamePad.Open(Config.UserId);
        }
        catch (ProsperoException exception)
        {
            EngineLog.Warning($"Controller open failed; 3D sample continues without input: {exception.Message}");
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        Renderer?.Dispose();
        Engine.Dispose();
        _gamePad?.Dispose();
        _display?.Dispose();
        UserService.sceUserServiceTerminate();
        GC.SuppressFinalize(this);
    }
}
