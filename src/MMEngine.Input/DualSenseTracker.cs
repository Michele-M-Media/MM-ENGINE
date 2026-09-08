namespace MMEngine.Input;

/// <summary>Edge detector for applications whose platform loop supplies snapshots directly.</summary>
public sealed class DualSenseTracker
{
    public DualSenseSnapshot Current { get; private set; } = DualSenseSnapshot.Neutral;
    public DualSenseSnapshot Previous { get; private set; } = DualSenseSnapshot.Neutral;

    public void Update(in DualSenseSnapshot snapshot)
    {
        Previous = Current;
        Current = snapshot;
    }

    public bool Held(DualSenseButtons buttons) => buttons != 0 && (Current.Buttons & buttons) == buttons;
    public bool Pressed(DualSenseButtons buttons) => Held(buttons) && (Previous.Buttons & buttons) != buttons;
    public bool Released(DualSenseButtons buttons) => buttons != 0 && (Previous.Buttons & buttons) == buttons && (Current.Buttons & buttons) != buttons;
}
