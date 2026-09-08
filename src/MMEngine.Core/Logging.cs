using System;

namespace MMEngine.Core;

public enum LogLevel
{
    Trace,
    Debug,
    Information,
    Warning,
    Error,
    None,
}

public interface ILogSink
{
    void Write(LogLevel level, string message);
}

public sealed class DelegateLogSink(Action<LogLevel, string> writer) : ILogSink
{
    private readonly Action<LogLevel, string> _writer = writer ?? throw new ArgumentNullException(nameof(writer));

    public void Write(LogLevel level, string message) => _writer(level, message);
}

/// <summary>Allocation-light engine logging with an injectable platform sink.</summary>
public static class EngineLog
{
    public static ILogSink? Sink { get; set; }
    public static LogLevel MinimumLevel { get; set; } = LogLevel.Information;

    public static void Write(LogLevel level, string message)
    {
        if (level < MinimumLevel || level == LogLevel.None)
            return;

        try
        {
            Sink?.Write(level, message ?? string.Empty);
        }
        catch
        {
            // Diagnostics must not take down the frame loop.
        }
    }

    public static void Trace(string message) => Write(LogLevel.Trace, message);
    public static void Debug(string message) => Write(LogLevel.Debug, message);
    public static void Information(string message) => Write(LogLevel.Information, message);
    public static void Warning(string message) => Write(LogLevel.Warning, message);
    public static void Error(string message) => Write(LogLevel.Error, message);
}
