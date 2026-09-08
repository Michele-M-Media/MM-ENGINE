using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace MMEngine.Assets;

/// <summary>Deterministic little-endian codec for .mmmesh v1.</summary>
public static class MmMeshCodec
{
    public const uint Magic = 0x48534D4D; // MMSH
    public const uint Version = 1;
    public const uint EndianMarker = 0x01020304;
    public const int HeaderSize = 64;
    public const int SubMeshRecordSize = 16;
    public const int MaterialRecordSize = 32;
    private const uint NoString = uint.MaxValue;

    public static byte[] Encode(MmMeshAsset mesh)
    {
        MmMeshValidator.ThrowIfInvalid(mesh);
        var strings = new StringTable();
        foreach (MmSubMesh subMesh in mesh.SubMeshes)
            strings.Add(subMesh.Name);
        foreach (MmMaterial material in mesh.Materials)
        {
            strings.Add(material.Name);
            if (material.BaseColorTexture is not null)
                strings.Add(material.BaseColorTexture.Replace('\\', '/'));
        }

        byte[] stringBytes = strings.Encode();
        int vertexOffset = HeaderSize;
        int indexOffset = CheckedAdd(vertexOffset, checked(mesh.Vertices.Length * MmVertex.SizeInBytes));
        int subMeshOffset = CheckedAdd(indexOffset, checked(mesh.Indices.Length * sizeof(uint)));
        int materialOffset = CheckedAdd(subMeshOffset, checked(mesh.SubMeshes.Length * SubMeshRecordSize));
        int stringOffset = CheckedAdd(materialOffset, checked(mesh.Materials.Length * MaterialRecordSize));
        int totalSize = CheckedAdd(stringOffset, stringBytes.Length);
        byte[] output = new byte[totalSize];
        Span<byte> bytes = output;

        WU32(bytes, 0, Magic);
        WU32(bytes, 4, Version);
        WU32(bytes, 8, EndianMarker);
        WU32(bytes, 12, HeaderSize);
        WU32(bytes, 16, MmVertex.SizeInBytes);
        WU32(bytes, 20, checked((uint)mesh.Vertices.Length));
        WU32(bytes, 24, checked((uint)mesh.Indices.Length));
        WU32(bytes, 28, checked((uint)mesh.SubMeshes.Length));
        WU32(bytes, 32, checked((uint)mesh.Materials.Length));
        WU32(bytes, 36, checked((uint)stringBytes.Length));
        WU32(bytes, 40, checked((uint)vertexOffset));
        WU32(bytes, 44, checked((uint)indexOffset));
        WU32(bytes, 48, checked((uint)subMeshOffset));
        WU32(bytes, 52, checked((uint)materialOffset));
        WU32(bytes, 56, checked((uint)stringOffset));
        WU32(bytes, 60, 0);

        int cursor = vertexOffset;
        foreach (MmVertex vertex in mesh.Vertices)
        {
            WF32(bytes, cursor, vertex.Position.X); WF32(bytes, cursor + 4, vertex.Position.Y); WF32(bytes, cursor + 8, vertex.Position.Z);
            WF32(bytes, cursor + 12, vertex.Normal.X); WF32(bytes, cursor + 16, vertex.Normal.Y); WF32(bytes, cursor + 20, vertex.Normal.Z);
            WF32(bytes, cursor + 24, vertex.TexCoord.X); WF32(bytes, cursor + 28, vertex.TexCoord.Y);
            WU32(bytes, cursor + 32, vertex.ColorArgb);
            cursor += MmVertex.SizeInBytes;
        }

        cursor = indexOffset;
        foreach (uint index in mesh.Indices)
        {
            WU32(bytes, cursor, index);
            cursor += sizeof(uint);
        }

        cursor = subMeshOffset;
        foreach (MmSubMesh subMesh in mesh.SubMeshes)
        {
            WU32(bytes, cursor, checked((uint)subMesh.FirstIndex));
            WU32(bytes, cursor + 4, checked((uint)subMesh.IndexCount));
            WU32(bytes, cursor + 8, checked((uint)subMesh.MaterialIndex));
            WU32(bytes, cursor + 12, strings.OffsetOf(subMesh.Name));
            cursor += SubMeshRecordSize;
        }

        cursor = materialOffset;
        foreach (MmMaterial material in mesh.Materials)
        {
            WU32(bytes, cursor, strings.OffsetOf(material.Name));
            WU32(bytes, cursor + 4, material.BaseColorTexture is null ? NoString : strings.OffsetOf(material.BaseColorTexture.Replace('\\', '/')));
            WU32(bytes, cursor + 8, material.BaseColorArgb);
            WU32(bytes, cursor + 12, (uint)material.Flags);
            WF32(bytes, cursor + 16, material.Metallic);
            WF32(bytes, cursor + 20, material.Roughness);
            WU32(bytes, cursor + 24, material.Sampler);
            WU32(bytes, cursor + 28, 0);
            cursor += MaterialRecordSize;
        }
        stringBytes.CopyTo(bytes[stringOffset..]);
        return output;
    }

    public static MmMeshAsset Decode(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < HeaderSize)
            throw Invalid("File is shorter than the .mmmesh header.");
        if (RU32(bytes, 0) != Magic)
            throw Invalid("Magic is not MMSH.");
        uint version = RU32(bytes, 4);
        if (version != Version)
            throw Invalid($"Unsupported .mmmesh version {version}; expected {Version}.");
        if (RU32(bytes, 8) != EndianMarker)
            throw Invalid("Unsupported byte order.");
        if (RU32(bytes, 12) != HeaderSize || RU32(bytes, 16) != MmVertex.SizeInBytes)
            throw Invalid("Header or vertex stride does not match .mmmesh v1.");
        if (RU32(bytes, 60) != 0)
            throw Invalid("Header flags are reserved in .mmmesh v1 and must be zero.");

        int vertexCount = Count(bytes, 20, "vertex");
        int indexCount = Count(bytes, 24, "index");
        int subMeshCount = Count(bytes, 28, "submesh");
        int materialCount = Count(bytes, 32, "material");
        int stringByteCount = Count(bytes, 36, "string byte");
        int vertexOffset = Offset(bytes, 40);
        int indexOffset = Offset(bytes, 44);
        int subMeshOffset = Offset(bytes, 48);
        int materialOffset = Offset(bytes, 52);
        int stringOffset = Offset(bytes, 56);

        int expectedIndexOffset = CheckedAdd(HeaderSize, checked(vertexCount * MmVertex.SizeInBytes));
        int expectedSubMeshOffset = CheckedAdd(expectedIndexOffset, checked(indexCount * sizeof(uint)));
        int expectedMaterialOffset = CheckedAdd(expectedSubMeshOffset, checked(subMeshCount * SubMeshRecordSize));
        int expectedStringOffset = CheckedAdd(expectedMaterialOffset, checked(materialCount * MaterialRecordSize));
        int expectedLength = CheckedAdd(expectedStringOffset, stringByteCount);
        if (vertexOffset != HeaderSize || indexOffset != expectedIndexOffset || subMeshOffset != expectedSubMeshOffset
            || materialOffset != expectedMaterialOffset || stringOffset != expectedStringOffset || bytes.Length != expectedLength)
            throw Invalid("Sections are not in canonical contiguous order or the file has trailing bytes.");

        RequireRange(bytes, vertexOffset, checked(vertexCount * MmVertex.SizeInBytes), "vertex data");
        RequireRange(bytes, indexOffset, checked(indexCount * sizeof(uint)), "index data");
        RequireRange(bytes, subMeshOffset, checked(subMeshCount * SubMeshRecordSize), "submesh data");
        RequireRange(bytes, materialOffset, checked(materialCount * MaterialRecordSize), "material data");
        RequireRange(bytes, stringOffset, stringByteCount, "string table");

        ReadOnlySpan<byte> stringTable = bytes.Slice(stringOffset, stringByteCount);
        var vertices = new MmVertex[vertexCount];
        int cursor = vertexOffset;
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = new MmVertex(
                new Vector3(RF32(bytes, cursor), RF32(bytes, cursor + 4), RF32(bytes, cursor + 8)),
                new Vector3(RF32(bytes, cursor + 12), RF32(bytes, cursor + 16), RF32(bytes, cursor + 20)),
                new Vector2(RF32(bytes, cursor + 24), RF32(bytes, cursor + 28)),
                RU32(bytes, cursor + 32));
            cursor += MmVertex.SizeInBytes;
        }

        var indices = new uint[indexCount];
        cursor = indexOffset;
        for (int i = 0; i < indices.Length; i++, cursor += sizeof(uint))
            indices[i] = RU32(bytes, cursor);

        var subMeshes = new MmSubMesh[subMeshCount];
        cursor = subMeshOffset;
        for (int i = 0; i < subMeshes.Length; i++, cursor += SubMeshRecordSize)
            subMeshes[i] = new MmSubMesh(
                ReadString(stringTable, RU32(bytes, cursor + 12)),
                checked((int)RU32(bytes, cursor)),
                checked((int)RU32(bytes, cursor + 4)),
                checked((int)RU32(bytes, cursor + 8)));

        var materials = new MmMaterial[materialCount];
        cursor = materialOffset;
        for (int i = 0; i < materials.Length; i++, cursor += MaterialRecordSize)
        {
            if (RU32(bytes, cursor + 28) != 0)
                throw Invalid($"Material {i} has non-zero reserved data.");
            uint textureOffset = RU32(bytes, cursor + 4);
            materials[i] = new MmMaterial(
                ReadString(stringTable, RU32(bytes, cursor)),
                RU32(bytes, cursor + 8),
                textureOffset == NoString ? null : ReadString(stringTable, textureOffset),
                (MmMaterialFlags)RU32(bytes, cursor + 12),
                RF32(bytes, cursor + 16),
                RF32(bytes, cursor + 20),
                RU32(bytes, cursor + 24));
        }
        return new MmMeshAsset(vertices, indices, subMeshes, materials);
    }

    private static void RequireRange(ReadOnlySpan<byte> bytes, int offset, int length, string name)
    {
        if (offset < HeaderSize || length < 0 || offset > bytes.Length - length)
            throw Invalid($"The {name} range is outside the file.");
    }

    private static int Count(ReadOnlySpan<byte> bytes, int offset, string name)
    {
        uint value = RU32(bytes, offset);
        if (value > int.MaxValue)
            throw Invalid($"The {name} count is too large.");
        return (int)value;
    }

    private static int Offset(ReadOnlySpan<byte> bytes, int offset) => checked((int)RU32(bytes, offset));
    private static int CheckedAdd(int left, int right) => checked(left + right);
    private static uint RU32(ReadOnlySpan<byte> bytes, int offset) => BinaryPrimitives.ReadUInt32LittleEndian(bytes[offset..]);
    private static float RF32(ReadOnlySpan<byte> bytes, int offset) => BinaryPrimitives.ReadSingleLittleEndian(bytes[offset..]);
    private static void WU32(Span<byte> bytes, int offset, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(bytes[offset..], value);
    private static void WF32(Span<byte> bytes, int offset, float value) => BinaryPrimitives.WriteSingleLittleEndian(bytes[offset..], value);
    private static InvalidOperationException Invalid(string message) => new($"Invalid .mmmesh: {message}");

    private static string ReadString(ReadOnlySpan<byte> table, uint offset)
    {
        if (offset >= table.Length)
            throw Invalid("A string offset is outside the string table.");
        ReadOnlySpan<byte> tail = table[(int)offset..];
        int terminator = tail.IndexOf((byte)0);
        if (terminator < 0)
            throw Invalid("A string is not null-terminated.");
        try
        {
            return new UTF8Encoding(false, true).GetString(tail[..terminator]);
        }
        catch (DecoderFallbackException exception)
        {
            throw Invalid($"String table is not valid UTF-8: {exception.Message}");
        }
    }

    private sealed class StringTable
    {
        private readonly List<string> _values = [];
        private readonly Dictionary<string, uint> _offsets = new(StringComparer.Ordinal);

        public void Add(string value)
        {
            value ??= string.Empty;
            if (!_offsets.ContainsKey(value))
            {
                _offsets.Add(value, 0);
                _values.Add(value);
            }
        }

        public uint OffsetOf(string value) => _offsets[value];

        public byte[] Encode()
        {
            var bytes = new List<byte>();
            foreach (string value in _values)
            {
                _offsets[value] = checked((uint)bytes.Count);
                bytes.AddRange(Encoding.UTF8.GetBytes(value));
                bytes.Add(0);
            }
            return [.. bytes];
        }
    }
}
