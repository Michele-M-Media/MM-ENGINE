using System.Numerics;
using MMEngine.Input;
using SharpProspero.Input;

namespace MMEngine.Platform.PS5;

public static class Ps5Input
{
    public static DualSenseSnapshot Convert(in GamePadState state)
    {
        (float lx, float ly) = state.LeftStick;
        (float rx, float ry) = state.RightStick;
        return new DualSenseSnapshot(
            state.IsConnected,
            (DualSenseButtons)(uint)state.Buttons,
            new Vector2(lx, ly),
            new Vector2(rx, ry),
            state.LeftTrigger / 255f,
            state.RightTrigger / 255f,
            state.Orientation,
            state.Acceleration,
            state.AngularVelocity,
            new TouchContact(state.Touch1.IsActive, state.Touch1.X, state.Touch1.Y, state.Touch1.Id),
            new TouchContact(state.Touch2.IsActive, state.Touch2.X, state.Touch2.Y, state.Touch2.Id),
            state.TimestampMicroseconds);
    }
}

/// <summary>Direct stable input source for code that owns its pad handle.</summary>
public sealed class Ps5DualSenseSource : IDualSenseSource
{
    private readonly GamePad _pad;
    public Ps5DualSenseSource(GamePad pad) => _pad = pad;
    public static Ps5DualSenseSource Open() => new(GamePad.Open());
    public DualSenseSnapshot Read()
    {
        GamePadState state = _pad.Read();
        return Ps5Input.Convert(state);
    }
    public bool SetVibration(byte largeMotor, byte smallMotor) => _pad.SetVibration(largeMotor, smallMotor);
    public bool SetLightBar(byte red, byte green, byte blue) => _pad.SetLightBar(red, green, blue);
    public void Dispose() => _pad.Dispose();
}
