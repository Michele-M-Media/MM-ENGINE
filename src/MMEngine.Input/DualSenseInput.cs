using System;

namespace MMEngine.Input;

public interface IDualSenseSource : IDisposable
{
    DualSenseSnapshot Read();
    bool SetVibration(byte largeMotor, byte smallMotor);
    bool SetLightBar(byte red, byte green, byte blue);
}

/// <summary>Frame-stable held/pressed/released API over any platform source.</summary>
public sealed class DualSenseInput : IDisposable
{
    private readonly IDualSenseSource _source;
    private bool _disposed;

    public DualSenseInput(IDualSenseSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        _source = source;
    }

    public DualSenseSnapshot Current { get; private set; } = DualSenseSnapshot.Neutral;
    public DualSenseSnapshot Previous { get; private set; } = DualSenseSnapshot.Neutral;

    public void Update()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Previous = Current;
        Current = _source.Read();
    }

    public bool Held(DualSenseButtons buttons) => buttons != 0 && (Current.Buttons & buttons) == buttons;
    public bool Pressed(DualSenseButtons buttons) => Held(buttons) && (Previous.Buttons & buttons) != buttons;
    public bool Released(DualSenseButtons buttons) => buttons != 0 && (Previous.Buttons & buttons) == buttons && (Current.Buttons & buttons) != buttons;
    public bool SetVibration(byte largeMotor, byte smallMotor) => _source.SetVibration(largeMotor, smallMotor);
    public bool SetLightBar(byte red, byte green, byte blue) => _source.SetLightBar(red, green, blue);

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _source.Dispose();
    }
}
