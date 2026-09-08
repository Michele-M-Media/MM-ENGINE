using MMEngine.Core;
using SharpProspero.Diagnostics;
using EngineLevel = MMEngine.Core.LogLevel;

namespace MMEngine.Platform.PS5;

public static class Ps5Logging
{
    public static void UseDevelopmentConsole()
    {
        SharpProspero.Diagnostics.Log.AddSink(new ConsoleLogSink());
        EngineLog.Sink = new DelegateLogSink(static (level, message) =>
        {
            SharpProspero.Diagnostics.Log.Write(level switch
            {
                EngineLevel.Trace => SharpProspero.Diagnostics.LogLevel.Trace,
                EngineLevel.Debug => SharpProspero.Diagnostics.LogLevel.Debug,
                EngineLevel.Information => SharpProspero.Diagnostics.LogLevel.Information,
                EngineLevel.Warning => SharpProspero.Diagnostics.LogLevel.Warning,
                EngineLevel.Error => SharpProspero.Diagnostics.LogLevel.Error,
                _ => SharpProspero.Diagnostics.LogLevel.None,
            }, message);
        });
    }
}
