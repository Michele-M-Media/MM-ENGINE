using System;

namespace MMEngine.Core;

/// <summary>Immutable timing data for one engine frame.</summary>
public readonly record struct FrameTime(long FrameIndex, double DeltaSeconds, double TotalSeconds)
{
    public float DeltaTime => (float)DeltaSeconds;
    public float TotalTime => (float)TotalSeconds;
}

/// <summary>Unity-style access to the frame currently being updated.</summary>
public static class Time
{
    private static FrameTime _current;

    public static FrameTime Current => _current;
    public static long FrameIndex => _current.FrameIndex;
    public static float DeltaTime => _current.DeltaTime;
    public static float TotalTime => _current.TotalTime;

    internal static void Set(in FrameTime value) => _current = value;
    internal static void Reset() => _current = default;
}

/// <summary>Produces monotonic, clamped frame timing from caller-supplied deltas.</summary>
public sealed class EngineClock
{
    private long _frameIndex;
    private double _totalSeconds;
    private double _maximumDeltaSeconds = 0.25;

    public double MaximumDeltaSeconds
    {
        get => _maximumDeltaSeconds;
        set
        {
            if (!double.IsFinite(value) || value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value), "Maximum frame delta must be finite and positive.");
            _maximumDeltaSeconds = value;
        }
    }

    public FrameTime Advance(double deltaSeconds)
    {
        if (!double.IsFinite(deltaSeconds) || deltaSeconds < 0)
            throw new ArgumentOutOfRangeException(nameof(deltaSeconds), "Frame delta must be finite and non-negative.");

        double clamped = Math.Min(deltaSeconds, _maximumDeltaSeconds);
        _totalSeconds += clamped;
        var result = new FrameTime(_frameIndex++, clamped, _totalSeconds);
        Time.Set(result);
        return result;
    }

    public void Reset()
    {
        _frameIndex = 0;
        _totalSeconds = 0;
        Time.Reset();
    }
}
