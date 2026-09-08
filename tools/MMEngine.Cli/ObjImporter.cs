using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using MMEngine.Assets;

namespace MMEngine.Cli;

internal static class ObjImporter
{
    private readonly record struct VertexKey(int Position, int TexCoord, int Normal);
    private sealed record PendingSubMesh(string Name, int FirstIndex, int IndexCount, int MaterialIndex);

    public static ImportResult Import(string path, float scale, bool flipV)
    {
        if (!float.IsFinite(scale) || scale == 0)
            throw new ArgumentOutOfRangeException(nameof(scale));
        var positions = new List<Vector3>();
        var positionColors = new List<uint>();
        var texCoords = new List<Vector2>();
        var normals = new List<Vector3>();
        var vertices = new List<MmVertex>();
        var missingNormals = new List<bool>();
        var indices = new List<uint>();
        var vertexMap = new Dictionary<VertexKey, uint>();
        var materials = new List<MmMaterial> { new("default", 0xFFFFFFFF, null) };
        var materialIndices = new Dictionary<string, int>(StringComparer.Ordinal) { ["default"] = 0 };
        var warnings = new List<string>();
        var subMeshes = new List<PendingSubMesh>();
        string groupName = "mesh";
        int materialIndex = 0;
        int sectionStart = 0;
        string baseDirectory = Path.GetDirectoryName(Path.GetFullPath(path)) ?? Directory.GetCurrentDirectory();

        void CloseSection()
        {
            int count = indices.Count - sectionStart;
            if (count > 0)
                subMeshes.Add(new PendingSubMesh(groupName, sectionStart, count, materialIndex));
            sectionStart = indices.Count;
        }

        foreach ((string rawLine, int lineNumber) in Lines(path))
        {
            string line = rawLine.Trim();
            if (line.Length == 0 || line[0] == '#')
                continue;
            string[] tokens = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            try
            {
                switch (tokens[0])
                {
                    case "v":
                        Require(tokens, 4, "v");
                        positions.Add(new Vector3(Parse.Float(tokens[1], "position"), Parse.Float(tokens[2], "position"), Parse.Float(tokens[3], "position")) * scale);
                        positionColors.Add(tokens.Length >= 7 ? PackColor(tokens[4], tokens[5], tokens[6], tokens.Length >= 8 ? tokens[7] : "1") : 0xFFFFFFFF);
                        break;
                    case "vt":
                        Require(tokens, 3, "vt");
                        float v = Parse.Float(tokens[2], "texture coordinate");
                        texCoords.Add(new Vector2(Parse.Float(tokens[1], "texture coordinate"), flipV ? 1f - v : v));
                        break;
                    case "vn":
                        Require(tokens, 4, "vn");
                        Vector3 normal = new(Parse.Float(tokens[1], "normal"), Parse.Float(tokens[2], "normal"), Parse.Float(tokens[3], "normal"));
                        normals.Add(normal.LengthSquared() > 1e-20f ? Vector3.Normalize(normal) : Vector3.Zero);
                        break;
                    case "o":
                    case "g":
                        CloseSection();
                        groupName = tokens.Length > 1 ? string.Join("_", tokens, 1, tokens.Length - 1) : "mesh";
                        break;
                    case "mtllib":
                        for (int i = 1; i < tokens.Length; i++)
                        {
                            string mtlPath = Path.Combine(baseDirectory, tokens[i]);
                            if (File.Exists(mtlPath))
                                MergeMaterials(ParseMtl(mtlPath, warnings), materials, materialIndices);
                            else
                                warnings.Add($"OBJ references missing material library '{tokens[i]}'; placeholder materials will be used.");
                        }
                        break;
                    case "usemtl":
                        CloseSection();
                        string materialName = tokens.Length > 1 ? string.Join(" ", tokens, 1, tokens.Length - 1) : "default";
                        if (!materialIndices.TryGetValue(materialName, out materialIndex))
                        {
                            materialIndex = materials.Count;
                            materialIndices.Add(materialName, materialIndex);
                            materials.Add(new MmMaterial(materialName, 0xFFFFFFFF, null));
                            warnings.Add($"Material '{materialName}' has no definition; using opaque white.");
                        }
                        break;
                    case "f":
                        Require(tokens, 4, "f");
                        var polygon = new uint[tokens.Length - 1];
                        for (int i = 1; i < tokens.Length; i++)
                            polygon[i - 1] = ResolveVertex(tokens[i], positions, texCoords, normals, positionColors, vertices, missingNormals, vertexMap);
                        for (int i = 1; i + 1 < polygon.Length; i++)
                        {
                            indices.Add(polygon[0]);
                            indices.Add(polygon[i]);
                            indices.Add(polygon[i + 1]);
                        }
                        break;
                }
            }
            catch (Exception exception) when (exception is FormatException or ArgumentException or IndexOutOfRangeException)
            {
                throw new FormatException($"{path}:{lineNumber}: {exception.Message}", exception);
            }
        }
        CloseSection();
        if (indices.Count == 0)
            throw new InvalidOperationException("OBJ contains no triangle faces.");
        GenerateMissingNormals(vertices, missingNormals, indices);
        var nativeSubMeshes = new MmSubMesh[subMeshes.Count];
        for (int i = 0; i < nativeSubMeshes.Length; i++)
        {
            PendingSubMesh sub = subMeshes[i];
            nativeSubMeshes[i] = new MmSubMesh(string.IsNullOrWhiteSpace(sub.Name) ? $"part_{i}" : sub.Name,
                sub.FirstIndex, sub.IndexCount, sub.MaterialIndex);
        }
        return new ImportResult(new MmMeshAsset(vertices, indices, nativeSubMeshes, materials), warnings);
    }

    private static uint ResolveVertex(string token, List<Vector3> positions, List<Vector2> texCoords,
        List<Vector3> normals, List<uint> positionColors, List<MmVertex> vertices, List<bool> missingNormals,
        Dictionary<VertexKey, uint> map)
    {
        string[] parts = token.Split('/');
        int p = ResolveIndex(Parse.Int(parts[0], "position index"), positions.Count, "position");
        int t = parts.Length > 1 && parts[1].Length > 0 ? ResolveIndex(Parse.Int(parts[1], "texture index"), texCoords.Count, "texture coordinate") : -1;
        int n = parts.Length > 2 && parts[2].Length > 0 ? ResolveIndex(Parse.Int(parts[2], "normal index"), normals.Count, "normal") : -1;
        var key = new VertexKey(p, t, n);
        if (map.TryGetValue(key, out uint existing))
            return existing;
        uint index = checked((uint)vertices.Count);
        vertices.Add(new MmVertex(positions[p], n >= 0 ? normals[n] : Vector3.Zero, t >= 0 ? texCoords[t] : Vector2.Zero, positionColors[p]));
        missingNormals.Add(n < 0 || normals[n].LengthSquared() <= 1e-20f);
        map.Add(key, index);
        return index;
    }

    private static int ResolveIndex(int index, int count, string kind)
    {
        int resolved = index > 0 ? index - 1 : count + index;
        if (index == 0 || resolved < 0 || resolved >= count)
            throw new IndexOutOfRangeException($"OBJ {kind} index {index} is outside 1..{count}.");
        return resolved;
    }

    private static void GenerateMissingNormals(List<MmVertex> vertices, List<bool> missing, List<uint> indices)
    {
        var sums = new Vector3[vertices.Count];
        for (int i = 0; i < indices.Count; i += 3)
        {
            int a = (int)indices[i], b = (int)indices[i + 1], c = (int)indices[i + 2];
            Vector3 face = Vector3.Cross(vertices[b].Position - vertices[a].Position, vertices[c].Position - vertices[a].Position);
            if (missing[a]) sums[a] += face;
            if (missing[b]) sums[b] += face;
            if (missing[c]) sums[c] += face;
        }
        for (int i = 0; i < vertices.Count; i++)
        {
            if (!missing[i])
                continue;
            Vector3 normal = sums[i].LengthSquared() > 1e-20f ? Vector3.Normalize(sums[i]) : Vector3.UnitY;
            vertices[i] = vertices[i] with { Normal = normal };
        }
    }

    private static List<MmMaterial> ParseMtl(string path, List<string> warnings)
    {
        var result = new List<MmMaterial>();
        string? name = null;
        Vector3 color = Vector3.One;
        float alpha = 1;
        string? texture = null;

        void Close()
        {
            if (name is null)
                return;
            uint packed = PackColor(color.X, color.Y, color.Z, alpha);
            MmMaterialFlags flags = alpha < 0.999f ? MmMaterialFlags.AlphaBlend : MmMaterialFlags.None;
            if (texture is not null)
                flags |= MmMaterialFlags.HasBaseColorTexture;
            result.Add(new MmMaterial(name, packed, texture, flags));
        }

        foreach ((string rawLine, _) in Lines(path))
        {
            string line = rawLine.Trim();
            if (line.Length == 0 || line[0] == '#')
                continue;
            string[] tokens = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            switch (tokens[0])
            {
                case "newmtl":
                    Close();
                    name = tokens.Length > 1 ? string.Join(" ", tokens, 1, tokens.Length - 1) : "material";
                    color = Vector3.One; alpha = 1; texture = null;
                    break;
                case "Kd" when tokens.Length >= 4:
                    color = new Vector3(Parse.Float(tokens[1], "Kd"), Parse.Float(tokens[2], "Kd"), Parse.Float(tokens[3], "Kd"));
                    break;
                case "d" when tokens.Length >= 2:
                    alpha = Math.Clamp(Parse.Float(tokens[1], "d"), 0, 1);
                    break;
                case "Tr" when tokens.Length >= 2:
                    alpha = 1 - Math.Clamp(Parse.Float(tokens[1], "Tr"), 0, 1);
                    break;
                case "map_Kd" when tokens.Length >= 2:
                    texture = tokens[^1].Replace('\\', '/');
                    break;
            }
        }
        Close();
        if (result.Count == 0)
            warnings.Add($"Material library '{Path.GetFileName(path)}' had no newmtl records.");
        return result;
    }

    private static void MergeMaterials(List<MmMaterial> incoming, List<MmMaterial> materials, Dictionary<string, int> indices)
    {
        foreach (MmMaterial material in incoming)
        {
            if (indices.TryGetValue(material.Name, out int existing))
                materials[existing] = material;
            else
            {
                indices.Add(material.Name, materials.Count);
                materials.Add(material);
            }
        }
    }

    private static uint PackColor(string r, string g, string b, string a)
        => PackColor(Parse.Float(r, "color"), Parse.Float(g, "color"), Parse.Float(b, "color"), Parse.Float(a, "alpha"));

    private static uint PackColor(float r, float g, float b, float a)
    {
        static uint B(float value) => (uint)(Math.Clamp(value, 0f, 1f) * 255f + 0.5f);
        return (B(a) << 24) | (B(r) << 16) | (B(g) << 8) | B(b);
    }

    private static void Require(string[] tokens, int minimum, string statement)
    {
        if (tokens.Length < minimum)
            throw new FormatException($"'{statement}' record has too few values.");
    }

    private static IEnumerable<(string Line, int Number)> Lines(string path)
    {
        int number = 0;
        foreach (string line in File.ReadLines(path))
            yield return (line, ++number);
    }
}
