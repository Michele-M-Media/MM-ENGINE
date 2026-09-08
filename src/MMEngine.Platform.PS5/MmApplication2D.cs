using MMEngine.Core;
using MMEngine.Graphics2D;
using MMEngine.Input;
using MMEngine.Scene;
using SharpProspero.Application;

namespace MMEngine.Platform.PS5;

/// <summary>Managed engine lifecycle on SharpProspero's verified CPU framebuffer loop.</summary>
public abstract class MmApplication2D : ProsperoApp
{
    protected MmApplication2D(AppConfig? config = null) : base(config)
        => Ps5Logging.UseDevelopmentConsole();

    public EngineRuntime Engine { get; } = new();
    public DualSenseTracker Input { get; } = new();
    protected Ps5Modules Modules { get; } = new();

    protected virtual bool RequiresPng => false;
    protected virtual bool RequiresTrueType => false;

    protected bool SetControllerVibration(byte largeMotor, byte smallMotor)
        => GamePad?.SetVibration(largeMotor, smallMotor) ?? false;

    protected bool SetControllerLightBar(byte red, byte green, byte blue)
        => GamePad?.SetLightBar(red, green, blue) ?? false;

    protected sealed override void OnLoad()
    {
        if (RequiresPng)
            Modules.LoadPng();
        if (RequiresTrueType)
            Modules.LoadTrueType();
        OnEngineLoad();
    }

    protected sealed override void OnFrame(FrameContext context)
    {
        DualSenseSnapshot snapshot = Ps5Input.Convert(context.Input);
        Input.Update(snapshot);
        FrameTime time = Engine.Tick(context.DeltaSeconds);
        OnEngineFrame(new Canvas2D(context.Surface, Engine.Diagnostics), time, context);
    }

    protected sealed override void OnUnload()
    {
        try
        {
            OnEngineUnload();
        }
        finally
        {
            Engine.Dispose();
            Modules.Dispose();
        }
    }

    protected virtual void OnEngineLoad() { }
    protected abstract void OnEngineFrame(Canvas2D canvas, in FrameTime time, FrameContext context);
    protected virtual void OnEngineUnload() { }
}
