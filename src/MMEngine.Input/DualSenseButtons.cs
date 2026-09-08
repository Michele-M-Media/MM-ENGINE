using System;

namespace MMEngine.Input;

/// <summary>Stable engine button names. Values match the verified SharpProspero pad mask.</summary>
[Flags]
public enum DualSenseButtons : uint
{
    None = 0,
    LeftStick = 0x00000002,
    RightStick = 0x00000004,
    Options = 0x00000008,
    DPadUp = 0x00000010,
    DPadRight = 0x00000020,
    DPadDown = 0x00000040,
    DPadLeft = 0x00000080,
    L2 = 0x00000100,
    R2 = 0x00000200,
    L1 = 0x00000400,
    R1 = 0x00000800,
    Triangle = 0x00001000,
    Circle = 0x00002000,
    Cross = 0x00004000,
    Square = 0x00008000,
    TouchPad = 0x00100000,
}
