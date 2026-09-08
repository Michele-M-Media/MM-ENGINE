using System;
using System.Collections.Generic;
using System.Numerics;

namespace MMEngine.Assets;

public static class MmMeshValidator
{
    public static IReadOnlyList<string> Validate(MmMeshAsset mesh)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        var errors = new List<string>();
        if (mesh.Vertices.Length == 0)
            errors.Add("Mesh has no vertices.");
        if (mesh.Indices.Length == 0 || mesh.Indices.Length % 3 != 0)
            errors.Add("Index count must be a non-zero multiple of three.");

        for (int i = 0; i < mesh.Vertices.Length; i++)
        {
            MmVertex vertex = mesh.Vertices[i];
            if (!Finite(vertex.Position) || !Finite(vertex.Normal) || !Finite(vertex.TexCoord))
                errors.Add($"Vertex {i} contains a non-finite value.");
        }

        for (int i = 0; i < mesh.Indices.Length; i++)
            if (mesh.Indices[i] >= mesh.Vertices.Length)
                errors.Add($"Index {i} points outside the vertex array ({mesh.Indices[i]} >= {mesh.Vertices.Length}).");

        if (mesh.SubMeshes.Length == 0)
            errors.Add("Mesh has no submeshes.");
        int expectedFirstIndex = 0;
        for (int i = 0; i < mesh.SubMeshes.Length; i++)
        {
            MmSubMesh sub = mesh.SubMeshes[i];
            if (sub.FirstIndex < 0 || sub.IndexCount <= 0 || sub.IndexCount % 3 != 0 ||
                (long)sub.FirstIndex + sub.IndexCount > mesh.Indices.Length)
                errors.Add($"Submesh {i} has an invalid index range.");
            if (sub.MaterialIndex < 0 || sub.MaterialIndex >= mesh.Materials.Length)
                errors.Add($"Submesh {i} refers to missing material {sub.MaterialIndex}.");
            if (sub.FirstIndex != expectedFirstIndex)
                errors.Add($"Submesh {i} starts at {sub.FirstIndex}; canonical contiguous order requires {expectedFirstIndex}.");
            if (sub.IndexCount > 0 && expectedFirstIndex <= int.MaxValue - sub.IndexCount)
                expectedFirstIndex += sub.IndexCount;
        }
        if (expectedFirstIndex != mesh.Indices.Length)
            errors.Add($"Submeshes cover {expectedFirstIndex} indices, but the mesh contains {mesh.Indices.Length}.");

        for (int i = 0; i < mesh.Materials.Length; i++)
        {
            MmMaterial material = mesh.Materials[i];
            if (string.IsNullOrWhiteSpace(material.Name))
                errors.Add($"Material {i} has no name.");
            if (!float.IsFinite(material.Metallic) || !float.IsFinite(material.Roughness))
                errors.Add($"Material {i} contains a non-finite scalar.");
            if (material.Metallic is < 0f or > 1f || material.Roughness is < 0f or > 1f)
                errors.Add($"Material {i} metallic/roughness must be in the 0..1 range.");
            bool textureFlag = (material.Flags & MmMaterialFlags.HasBaseColorTexture) != 0;
            if (textureFlag != (material.BaseColorTexture is not null))
                errors.Add($"Material {i} texture flag and texture name disagree.");
        }
        return errors;
    }

    public static void ThrowIfInvalid(MmMeshAsset mesh)
    {
        IReadOnlyList<string> errors = Validate(mesh);
        if (errors.Count != 0)
            throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
    }

    private static bool Finite(Vector2 value) => float.IsFinite(value.X) && float.IsFinite(value.Y);
    private static bool Finite(Vector3 value) => float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
}
