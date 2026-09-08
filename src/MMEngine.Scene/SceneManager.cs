using System;
using MMEngine.Core;

namespace MMEngine.Scene;

public sealed class SceneManager : IDisposable
{
    public Scene? ActiveScene { get; private set; }

    public void SetActive(Scene scene, bool disposePrevious = true)
    {
        ArgumentNullException.ThrowIfNull(scene);
        Scene? previous = ActiveScene;
        ActiveScene = scene;
        if (disposePrevious && previous is not null && !ReferenceEquals(previous, scene))
            previous.Dispose();
    }

    public void Update(in FrameTime time, EngineDiagnostics diagnostics)
        => ActiveScene?.Update(time, diagnostics);

    public void Dispose()
    {
        ActiveScene?.Dispose();
        ActiveScene = null;
    }
}
