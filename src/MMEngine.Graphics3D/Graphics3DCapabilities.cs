namespace MMEngine.Graphics3D;

/// <summary>Capabilities proven against the unmodified SharpProspero 0.8 source in the bootstrap.</summary>
public static class Graphics3DCapabilities
{
    public const bool MultiMesh = true;
    public const bool VertexColor = true;
    public const bool UvPreservedInAssets = true;
    public const bool MaterialMetadataPreserved = true;
    public const bool TextureSampling = false;
    public const bool DepthBuffer = false;

    public const string TextureSamplingBlocker =
        "The supplied SDK has GNF encoding and texture/sampler descriptors, but no verified pixel-shader resource binding path or shader compiler.";
    public const string DepthBufferBlocker =
        "The supplied SDK exposes depth types and tiling math but not a verified depth target register-default block and clear/bind sequence.";
}
