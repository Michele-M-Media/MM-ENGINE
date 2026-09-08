namespace MMEngine.Core;

/// <summary>Counters reset at the start of every frame and populated by engine subsystems.</summary>
public sealed class EngineDiagnostics
{
    public long FrameIndex { get; private set; }
    public int ActiveGameObjects { get; set; }
    public int UpdatedComponents { get; set; }
    public int DrawCalls2D { get; set; }
    public int DrawCalls3D { get; set; }
    public long Triangles { get; set; }

    public void BeginFrame(long frameIndex)
    {
        FrameIndex = frameIndex;
        ActiveGameObjects = 0;
        UpdatedComponents = 0;
        DrawCalls2D = 0;
        DrawCalls3D = 0;
        Triangles = 0;
    }
}
