using System;

namespace MMEngine.Animation;

public enum Ease
{
    Linear,
    SmoothStep,
    EaseIn,
    EaseOut,
    EaseInOut,
}

public sealed class Tween
{
    private readonly Action<float> _setter;

    public Tween(float from, float to, double durationSeconds, Action<float> setter, Ease ease = Ease.SmoothStep)
    {
        if (!float.IsFinite(from) || !float.IsFinite(to))
            throw new ArgumentOutOfRangeException(nameof(from), "Tween endpoints must be finite.");
        if (!double.IsFinite(durationSeconds) || durationSeconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(durationSeconds));
        ArgumentNullException.ThrowIfNull(setter);
        From = from;
        To = to;
        DurationSeconds = durationSeconds;
        _setter = setter;
        Easing = ease;
    }

    public float From { get; }
    public float To { get; }
    public double DurationSeconds { get; }
    public double ElapsedSeconds { get; private set; }
    public Ease Easing { get; }
    public bool Loop { get; set; }
    public bool IsComplete { get; private set; }

    public void Reset()
    {
        ElapsedSeconds = 0;
        IsComplete = false;
        _setter(From);
    }

    public void Update(double deltaSeconds)
    {
        if (IsComplete || deltaSeconds <= 0)
            return;
        ElapsedSeconds += deltaSeconds;
        double phase = ElapsedSeconds / DurationSeconds;
        if (Loop)
        {
            phase -= Math.Floor(phase);
            ElapsedSeconds %= DurationSeconds;
        }
        else if (phase >= 1)
        {
            phase = 1;
            IsComplete = true;
        }
        float amount = Apply((float)phase, Easing);
        _setter(From + (To - From) * amount);
    }

    public static float Apply(float value, Ease ease)
    {
        float t = Math.Clamp(value, 0f, 1f);
        return ease switch
        {
            Ease.Linear => t,
            Ease.SmoothStep => t * t * (3f - 2f * t),
            Ease.EaseIn => t * t,
            Ease.EaseOut => 1f - (1f - t) * (1f - t),
            Ease.EaseInOut => t < 0.5f ? 2f * t * t : 1f - MathF.Pow(-2f * t + 2f, 2f) / 2f,
            _ => t,
        };
    }
}
