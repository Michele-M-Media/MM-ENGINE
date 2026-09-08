using System.Numerics;

namespace MMEngine.Input;

public readonly record struct TouchContact(bool Active, ushort X, ushort Y, byte Id);

/// <summary>Immutable, normalized input state captured once per engine frame.</summary>
public readonly record struct DualSenseSnapshot(
    bool Connected,
    DualSenseButtons Buttons,
    Vector2 LeftStick,
    Vector2 RightStick,
    float LeftTrigger,
    float RightTrigger,
    Quaternion Orientation,
    Vector3 Acceleration,
    Vector3 AngularVelocity,
    TouchContact Touch1,
    TouchContact Touch2,
    ulong TimestampMicroseconds)
{
    public static DualSenseSnapshot Neutral => new(
        false, DualSenseButtons.None, Vector2.Zero, Vector2.Zero, 0, 0,
        Quaternion.Identity, Vector3.Zero, Vector3.Zero, default, default, 0);
}
