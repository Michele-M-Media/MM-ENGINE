using System;
using System.Collections.Generic;
using System.Numerics;

namespace MMEngine.Assets;

/// <summary>Engine-native vertex layout. It intentionally matches SharpProspero.Vertex (36 bytes).</summary>
public readonly record struct MmVertex(Vector3 Position, Vector3 Normal, Vector2 TexCoord, uint ColorArgb)
{
    public const int SizeInBytes = 36;
}

[Flags]
public enum MmMaterialFlags : uint
{
    None = 0,
    DoubleSided = 1 << 0,
    AlphaBlend = 1 << 1,
    HasBaseColorTexture = 1 << 2,
}

public readonly record struct MmMaterial(
    string Name,
    uint BaseColorArgb,
    string? BaseColorTexture,
    MmMaterialFlags Flags = MmMaterialFlags.None,
    float Metallic = 0f,
    float Roughness = 1f,
    uint Sampler = 0);

public readonly record struct MmSubMesh(string Name, int FirstIndex, int IndexCount, int MaterialIndex);

/// <summary>A complete multi-mesh asset with UV and material metadata retained.</summary>
public sealed class MmMeshAsset
{
    public MmMeshAsset(
        IReadOnlyList<MmVertex> vertices,
        IReadOnlyList<uint> indices,
        IReadOnlyList<MmSubMesh> subMeshes,
        IReadOnlyList<MmMaterial> materials)
    {
        ArgumentNullException.ThrowIfNull(vertices);
        ArgumentNullException.ThrowIfNull(indices);
        ArgumentNullException.ThrowIfNull(subMeshes);
        ArgumentNullException.ThrowIfNull(materials);
        Vertices = Copy(vertices);
        Indices = Copy(indices);
        SubMeshes = Copy(subMeshes);
        Materials = Copy(materials);
        MmMeshValidator.ThrowIfInvalid(this);
    }

    public MmVertex[] Vertices { get; }
    public uint[] Indices { get; }
    public MmSubMesh[] SubMeshes { get; }
    public MmMaterial[] Materials { get; }

    private static T[] Copy<T>(IReadOnlyList<T> source)
    {
        var result = new T[source.Count];
        for (int i = 0; i < result.Length; i++)
            result[i] = source[i];
        return result;
    }
}
