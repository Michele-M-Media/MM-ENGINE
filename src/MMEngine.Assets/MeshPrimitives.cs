using System.Collections.Generic;
using System.Numerics;

namespace MMEngine.Assets;

public static class MeshPrimitives
{
    public static MmMeshAsset Cube(uint colorArgb = 0xFFFFFFFF)
    {
        var vertices = new List<MmVertex>(24);
        var indices = new List<uint>(36);
        (Vector3 Normal, Vector3 U, Vector3 V)[] faces =
        [
            (Vector3.UnitZ, Vector3.UnitX, Vector3.UnitY),
            (-Vector3.UnitZ, -Vector3.UnitX, Vector3.UnitY),
            (Vector3.UnitX, -Vector3.UnitZ, Vector3.UnitY),
            (-Vector3.UnitX, Vector3.UnitZ, Vector3.UnitY),
            (Vector3.UnitY, Vector3.UnitX, -Vector3.UnitZ),
            (-Vector3.UnitY, Vector3.UnitX, Vector3.UnitZ),
        ];
        foreach ((Vector3 normal, Vector3 u, Vector3 v) in faces)
        {
            uint start = (uint)vertices.Count;
            Vector3 center = normal * 0.5f;
            vertices.Add(new(center - u * 0.5f - v * 0.5f, normal, new(0, 1), colorArgb));
            vertices.Add(new(center + u * 0.5f - v * 0.5f, normal, new(1, 1), colorArgb));
            vertices.Add(new(center + u * 0.5f + v * 0.5f, normal, new(1, 0), colorArgb));
            vertices.Add(new(center - u * 0.5f + v * 0.5f, normal, new(0, 0), colorArgb));
            indices.Add(start); indices.Add(start + 1); indices.Add(start + 2);
            indices.Add(start); indices.Add(start + 2); indices.Add(start + 3);
        }
        return SingleMaterial(vertices, indices, "cube");
    }

    public static MmMeshAsset Quad(uint colorArgb = 0xFFFFFFFF)
    {
        MmVertex[] vertices =
        [
            new(new(-0.5f, -0.5f, 0), Vector3.UnitZ, new(0, 1), colorArgb),
            new(new( 0.5f, -0.5f, 0), Vector3.UnitZ, new(1, 1), colorArgb),
            new(new( 0.5f,  0.5f, 0), Vector3.UnitZ, new(1, 0), colorArgb),
            new(new(-0.5f,  0.5f, 0), Vector3.UnitZ, new(0, 0), colorArgb),
        ];
        return SingleMaterial(vertices, new uint[] { 0, 1, 2, 0, 2, 3 }, "quad");
    }

    private static MmMeshAsset SingleMaterial(IReadOnlyList<MmVertex> vertices, IReadOnlyList<uint> indices, string name)
        => new(vertices, indices, [new MmSubMesh(name, 0, indices.Count, 0)], [new MmMaterial("default", 0xFFFFFFFF, null)]);
}
