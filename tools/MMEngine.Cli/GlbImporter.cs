using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text.Json;
using MMEngine.Assets;

namespace MMEngine.Cli;

internal static class GlbImporter
{
    private const uint GlbMagic = 0x46546C67;
    private const uint JsonChunk = 0x4E4F534A;
    private const uint BinChunk = 0x004E4942;

    public static ImportResult Import(string path, float scale, bool flipV)
    {
        if (!float.IsFinite(scale) || scale == 0)
            throw new ArgumentOutOfRangeException(nameof(scale));
        byte[] file = File.ReadAllBytes(path);
        (byte[] json, byte[] binary) = ReadContainer(file);
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;
        if (root.TryGetProperty("animations", out JsonElement animations) && animations.GetArrayLength() != 0)
            throw new NotSupportedException("GLB animations are not supported; bake the desired pose before import.");
        if (root.TryGetProperty("skins", out JsonElement skins) && skins.GetArrayLength() != 0)
            throw new NotSupportedException("GLB skins are not supported; bake skinned geometry before import.");
        if (!root.TryGetProperty("buffers", out JsonElement buffers)
            || buffers.GetArrayLength() != 1
            || buffers[0].TryGetProperty("uri", out _))
            throw new NotSupportedException("GLB must contain exactly one embedded BIN buffer.");
        int declaredBinaryLength = buffers[0].GetProperty("byteLength").GetInt32();
        if (declaredBinaryLength < 0 || binary.Length < declaredBinaryLength || binary.Length - declaredBinaryLength > 3)
            throw new FormatException("The embedded BIN chunk length does not match buffers[0].byteLength (apart from legal padding).");
        if (root.TryGetProperty("extensionsRequired", out JsonElement requiredExtensions))
        {
            foreach (JsonElement extension in requiredExtensions.EnumerateArray())
            {
                string name = extension.GetString() ?? throw new FormatException("A required GLB extension name is null.");
                if (name != "KHR_texture_transform")
                    throw new NotSupportedException($"Required GLB extension '{name}' is not supported.");
            }
        }
        var context = new Context(root, binary, flipV);
        context.ReadMaterials();

        Matrix4x4 scaleMatrix = Matrix4x4.CreateScale(scale);
        if (root.TryGetProperty("scenes", out JsonElement scenes) && scenes.GetArrayLength() > 0)
        {
            int sceneIndex = root.TryGetProperty("scene", out JsonElement active) ? active.GetInt32() : 0;
            JsonElement scene = scenes[sceneIndex];
            if (scene.TryGetProperty("nodes", out JsonElement roots))
                foreach (JsonElement node in roots.EnumerateArray())
                    context.ProcessNode(node.GetInt32(), scaleMatrix, new HashSet<int>());
        }
        else if (root.TryGetProperty("nodes", out JsonElement nodes))
        {
            var children = new HashSet<int>();
            foreach (JsonElement node in nodes.EnumerateArray())
                if (node.TryGetProperty("children", out JsonElement list))
                    foreach (JsonElement child in list.EnumerateArray())
                        children.Add(child.GetInt32());
            for (int i = 0; i < nodes.GetArrayLength(); i++)
                if (!children.Contains(i))
                    context.ProcessNode(i, scaleMatrix, new HashSet<int>());
        }
        else if (root.TryGetProperty("meshes", out JsonElement meshes))
        {
            for (int i = 0; i < meshes.GetArrayLength(); i++)
                context.ProcessMesh(i, Matrix4x4.CreateScale(scale), $"mesh_{i}");
        }

        if (context.Indices.Count == 0)
            throw new InvalidOperationException("GLB contains no triangle primitives in its active scene.");
        return new ImportResult(
            new MmMeshAsset(context.Vertices, context.Indices, context.SubMeshes, context.Materials),
            context.Warnings);
    }

    private static (byte[] Json, byte[] Binary) ReadContainer(ReadOnlySpan<byte> file)
    {
        if (file.Length < 20 || BinaryPrimitives.ReadUInt32LittleEndian(file) != GlbMagic)
            throw new FormatException("Not a binary glTF file.");
        uint version = BinaryPrimitives.ReadUInt32LittleEndian(file[4..]);
        if (version != 2)
            throw new FormatException($"Unsupported GLB version {version}; expected 2.");
        uint declaredLength = BinaryPrimitives.ReadUInt32LittleEndian(file[8..]);
        if (declaredLength != file.Length)
            throw new FormatException("GLB declared length does not match the file.");
        byte[]? json = null;
        byte[]? binary = null;
        int offset = 12;
        while (offset <= file.Length - 8)
        {
            int length = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(file[offset..]));
            uint type = BinaryPrimitives.ReadUInt32LittleEndian(file[(offset + 4)..]);
            offset += 8;
            if (length < 0 || offset > file.Length - length)
                throw new FormatException("GLB chunk runs outside the file.");
            if ((length & 3) != 0)
                throw new FormatException("GLB chunks must be aligned to four bytes.");
            if (type == JsonChunk)
            {
                if (json is not null)
                    throw new FormatException("GLB contains more than one JSON chunk.");
                json = file.Slice(offset, length).ToArray();
            }
            else if (type == BinChunk)
            {
                if (binary is not null)
                    throw new FormatException("GLB contains more than one BIN chunk.");
                binary = file.Slice(offset, length).ToArray();
            }
            offset += length;
        }
        if (offset != file.Length)
            throw new FormatException("GLB has incomplete trailing chunk data.");
        return (json ?? throw new FormatException("GLB has no JSON chunk."),
            binary ?? throw new FormatException("GLB has no BIN chunk."));
    }

    private sealed class Context
    {
        private readonly JsonElement _root;
        private readonly byte[] _binary;
        private readonly bool _flipV;

        public Context(JsonElement root, byte[] binary, bool flipV)
        {
            _root = root;
            _binary = binary;
            _flipV = flipV;
        }

        public List<MmVertex> Vertices { get; } = [];
        public List<uint> Indices { get; } = [];
        public List<MmSubMesh> SubMeshes { get; } = [];
        public List<MmMaterial> Materials { get; } = [new MmMaterial("default", 0xFFFFFFFF, null)];
        private List<TextureTransform> TextureTransforms { get; } = [TextureTransform.Identity];
        public List<string> Warnings { get; } = [];

        public void ReadMaterials()
        {
            if (!_root.TryGetProperty("materials", out JsonElement sourceMaterials))
                return;
            int index = 0;
            foreach (JsonElement source in sourceMaterials.EnumerateArray())
            {
                string name = source.TryGetProperty("name", out JsonElement n) ? n.GetString() ?? $"material_{index}" : $"material_{index}";
                float r = 1, g = 1, b = 1, a = 1;
                string? texture = null;
                TextureTransform textureTransform = TextureTransform.Identity;
                if (source.TryGetProperty("pbrMetallicRoughness", out JsonElement pbr))
                {
                    if (pbr.TryGetProperty("baseColorFactor", out JsonElement color))
                    {
                        if (color.GetArrayLength() != 4)
                            throw new FormatException($"Material '{name}' baseColorFactor must contain four numbers.");
                        r = color[0].GetSingle(); g = color[1].GetSingle(); b = color[2].GetSingle(); a = color[3].GetSingle();
                        if (!Unit(r) || !Unit(g) || !Unit(b) || !Unit(a))
                            throw new FormatException($"Material '{name}' baseColorFactor must contain finite values in the 0..1 range.");
                    }
                    if (pbr.TryGetProperty("baseColorTexture", out JsonElement textureInfo))
                    {
                        int texCoord = textureInfo.TryGetProperty("texCoord", out JsonElement texCoordSet) ? texCoordSet.GetInt32() : 0;
                        if (textureInfo.TryGetProperty("extensions", out JsonElement textureExtensions)
                            && textureExtensions.TryGetProperty("KHR_texture_transform", out JsonElement transform))
                        {
                            if (transform.TryGetProperty("texCoord", out JsonElement transformedSet))
                                texCoord = transformedSet.GetInt32();
                            Vector2 offset = ReadVector2(transform, "offset", Vector2.Zero);
                            Vector2 textureScale = ReadVector2(transform, "scale", Vector2.One);
                            float rotation = transform.TryGetProperty("rotation", out JsonElement angle) ? angle.GetSingle() : 0f;
                            if (!float.IsFinite(rotation))
                                throw new FormatException("KHR_texture_transform rotation must be finite.");
                            textureTransform = new TextureTransform(offset, textureScale, rotation);
                        }
                        if (textureInfo.TryGetProperty("extensions", out textureExtensions))
                            foreach (JsonProperty extension in textureExtensions.EnumerateObject())
                                if (extension.Name != "KHR_texture_transform")
                                    Warnings.Add($"Material '{name}' base-color texture extension '{extension.Name}' is not represented in .mmmesh v1.");
                        if (texCoord != 0)
                            throw new NotSupportedException("Only base-color TEXCOORD_0 is supported.");
                        texture = TextureName(textureInfo.GetProperty("index").GetInt32());
                    }
                }
                float metallic = pbrValue(source, "metallicFactor", 1f);
                float roughness = pbrValue(source, "roughnessFactor", 1f);
                if (!Unit(metallic) || !Unit(roughness))
                    throw new FormatException($"Material '{name}' metallic/roughness factors must be finite values in the 0..1 range.");
                MmMaterialFlags flags = MmMaterialFlags.None;
                if (source.TryGetProperty("doubleSided", out JsonElement doubleSided) && doubleSided.GetBoolean())
                    flags |= MmMaterialFlags.DoubleSided;
                if (source.TryGetProperty("alphaMode", out JsonElement alphaMode))
                {
                    switch (alphaMode.GetString())
                    {
                        case "OPAQUE": break;
                        case "BLEND": flags |= MmMaterialFlags.AlphaBlend; break;
                        case "MASK": Warnings.Add($"Material '{name}' alpha MASK is retained as an opaque fallback; alpha cutoff is not represented in .mmmesh v1."); break;
                        default: throw new FormatException($"Material '{name}' has an invalid alphaMode.");
                    }
                }
                if (source.TryGetProperty("normalTexture", out _)
                    || source.TryGetProperty("occlusionTexture", out _)
                    || source.TryGetProperty("emissiveTexture", out _)
                    || source.TryGetProperty("emissiveFactor", out _))
                    Warnings.Add($"Material '{name}' has non-base-color maps or emissive data that .mmmesh v1 does not represent.");
                if (source.TryGetProperty("extensions", out JsonElement materialExtensions))
                    foreach (JsonProperty extension in materialExtensions.EnumerateObject())
                        Warnings.Add($"Material '{name}' extension '{extension.Name}' is not represented in .mmmesh v1.");
                if (texture is not null)
                {
                    flags |= MmMaterialFlags.HasBaseColorTexture;
                    Warnings.Add($"Material '{name}' texture metadata is preserved; the current PS5 renderer uses vertex/base color fallback.");
                }
                Materials.Add(new MmMaterial(name, Pack(r, g, b, a), texture, flags, metallic, roughness));
                TextureTransforms.Add(textureTransform);
                index++;

                float pbrValue(JsonElement material, string property, float fallback)
                {
                    if (material.TryGetProperty("pbrMetallicRoughness", out JsonElement values) && values.TryGetProperty(property, out JsonElement value))
                        return value.GetSingle();
                    return fallback;
                }
            }
        }

        public void ProcessNode(int nodeIndex, Matrix4x4 parent, HashSet<int> stack)
        {
            if (!stack.Add(nodeIndex))
                throw new FormatException($"GLB node graph contains a cycle at node {nodeIndex}.");
            JsonElement nodes = _root.GetProperty("nodes");
            if ((uint)nodeIndex >= (uint)nodes.GetArrayLength())
                throw new FormatException($"GLB node index {nodeIndex} is out of range.");
            JsonElement node = nodes[nodeIndex];
            Matrix4x4 world = NodeMatrix(node) * parent;
            string name = node.TryGetProperty("name", out JsonElement n) ? n.GetString() ?? $"node_{nodeIndex}" : $"node_{nodeIndex}";
            if (node.TryGetProperty("mesh", out JsonElement mesh))
                ProcessMesh(mesh.GetInt32(), world, name);
            if (node.TryGetProperty("children", out JsonElement children))
                foreach (JsonElement child in children.EnumerateArray())
                    ProcessNode(child.GetInt32(), world, stack);
            stack.Remove(nodeIndex);
        }

        public void ProcessMesh(int meshIndex, Matrix4x4 transform, string nodeName)
        {
            JsonElement meshes = _root.GetProperty("meshes");
            if ((uint)meshIndex >= (uint)meshes.GetArrayLength())
                throw new FormatException($"GLB mesh index {meshIndex} is out of range.");
            JsonElement mesh = meshes[meshIndex];
            string meshName = mesh.TryGetProperty("name", out JsonElement n) ? n.GetString() ?? nodeName : nodeName;
            if (!Matrix4x4.Invert(transform, out Matrix4x4 inverse))
                throw new FormatException($"Node transform for '{nodeName}' is not invertible.");
            Matrix4x4 normalMatrix = Matrix4x4.Transpose(inverse);
            int primitiveIndex = 0;
            foreach (JsonElement primitive in mesh.GetProperty("primitives").EnumerateArray())
            {
                if (primitive.TryGetProperty("mode", out JsonElement mode) && mode.GetInt32() != 4)
                    throw new NotSupportedException($"GLB primitive mode {mode.GetInt32()} is not TRIANGLES.");
                if (primitive.TryGetProperty("extensions", out JsonElement extensions) && extensions.TryGetProperty("KHR_draco_mesh_compression", out _))
                    throw new NotSupportedException("KHR_draco_mesh_compression is not supported; export an uncompressed GLB.");
                if (primitive.TryGetProperty("targets", out _))
                    throw new NotSupportedException("GLB morph targets are not supported; bake the desired shape before import.");

                JsonElement attributes = primitive.GetProperty("attributes");
                Accessor positions = GetAccessor(attributes.GetProperty("POSITION").GetInt32());
                if (positions.Count == 0 || positions.Type != "VEC3" || positions.ComponentType != 5126 || positions.Normalized)
                    throw new NotSupportedException("POSITION must be a float VEC3 accessor.");
                Accessor? normals = attributes.TryGetProperty("NORMAL", out JsonElement normalIndex) ? GetAccessor(normalIndex.GetInt32()) : null;
                Accessor? texCoords = attributes.TryGetProperty("TEXCOORD_0", out JsonElement texIndex) ? GetAccessor(texIndex.GetInt32()) : null;
                Accessor? colors = attributes.TryGetProperty("COLOR_0", out JsonElement colorIndex) ? GetAccessor(colorIndex.GetInt32()) : null;
                int material = primitive.TryGetProperty("material", out JsonElement materialIndex) ? materialIndex.GetInt32() + 1 : 0;
                if ((uint)material >= (uint)Materials.Count)
                    throw new FormatException($"Primitive refers to missing material {material - 1}.");
                if (Materials[material].BaseColorTexture is not null && texCoords is null)
                    Warnings.Add($"Primitive '{meshName}/{primitiveIndex}' has a base-color texture but no TEXCOORD_0; zero UV fallback is stored.");
                if (normals is not null && (normals.Count != positions.Count || normals.Type != "VEC3"
                    || normals.ComponentType is not (5120 or 5122 or 5126)
                    || (normals.ComponentType != 5126 && !normals.Normalized)))
                    throw new NotSupportedException("NORMAL must match POSITION and use float or normalized signed VEC3 data.");
                if (texCoords is not null && (texCoords.Count != positions.Count || texCoords.Type != "VEC2"
                    || texCoords.ComponentType is not (5121 or 5123 or 5126)
                    || (texCoords.ComponentType != 5126 && !texCoords.Normalized)))
                    throw new NotSupportedException("TEXCOORD_0 must match POSITION and use float or normalized unsigned VEC2 data.");
                if (colors is not null && (colors.Count != positions.Count || colors.Type is not ("VEC3" or "VEC4")
                    || colors.ComponentType is not (5121 or 5123 or 5126)
                    || (colors.ComponentType != 5126 && !colors.Normalized)))
                    throw new NotSupportedException("COLOR_0 must match POSITION and use float or normalized unsigned VEC3/VEC4 data.");
                int firstVertex = Vertices.Count;
                for (int i = 0; i < positions.Count; i++)
                {
                    Vector4 p = positions.Read(i);
                    Vector3 position = Vector3.Transform(new Vector3(p.X, p.Y, p.Z), transform);
                    Vector3 normal = Vector3.UnitY;
                    if (normals is not null)
                    {
                        Vector4 sourceNormal = normals.Read(i);
                        Vector3 transformed = Vector3.TransformNormal(new Vector3(sourceNormal.X, sourceNormal.Y, sourceNormal.Z), normalMatrix);
                        normal = transformed.LengthSquared() > 1e-20f ? Vector3.Normalize(transformed) : Vector3.UnitY;
                    }
                    Vector2 uv = Vector2.Zero;
                    if (texCoords is not null)
                    {
                        Vector4 sourceUv = texCoords.Read(i);
                        uv = TextureTransforms[material].Apply(new Vector2(sourceUv.X, sourceUv.Y));
                        if (_flipV)
                            uv.Y = 1f - uv.Y;
                    }
                    uint color = 0xFFFFFFFF;
                    if (colors is not null)
                    {
                        Vector4 c = colors.Read(i, defaultW: 1f);
                        color = Pack(c.X, c.Y, c.Z, c.W);
                    }
                    Vertices.Add(new MmVertex(position, normal, uv, color));
                }

                int firstIndex = Indices.Count;
                if (primitive.TryGetProperty("indices", out JsonElement indicesAccessor))
                {
                    Accessor sourceIndices = GetAccessor(indicesAccessor.GetInt32());
                    if (sourceIndices.Type != "SCALAR" || sourceIndices.Normalized
                        || sourceIndices.ComponentType is not (5121 or 5123 or 5125))
                        throw new NotSupportedException("Indices must be unsigned byte, unsigned short, or unsigned int SCALAR values.");
                    for (int i = 0; i < sourceIndices.Count; i++)
                    {
                        uint localIndex = sourceIndices.ReadUnsigned(i);
                        if (localIndex >= positions.Count)
                            throw new FormatException($"Primitive index {localIndex} is outside its {positions.Count} vertices.");
                        Indices.Add(checked((uint)firstVertex + localIndex));
                    }
                }
                else
                {
                    for (int i = 0; i < positions.Count; i++)
                        Indices.Add(checked((uint)(firstVertex + i)));
                }
                int indexCount = Indices.Count - firstIndex;
                if (indexCount == 0 || indexCount % 3 != 0)
                    throw new FormatException($"Primitive '{meshName}/{primitiveIndex}' has a non-triangle index count.");
                if (normals is null)
                    GenerateNormals(firstVertex, positions.Count, firstIndex, indexCount);
                SubMeshes.Add(new MmSubMesh($"{meshName}_{primitiveIndex}", firstIndex, indexCount, material));
                primitiveIndex++;
            }
        }

        private void GenerateNormals(int firstVertex, int vertexCount, int firstIndex, int indexCount)
        {
            var sums = new Vector3[vertexCount];
            for (int i = firstIndex; i < firstIndex + indexCount; i += 3)
            {
                int a = checked((int)Indices[i]) - firstVertex;
                int b = checked((int)Indices[i + 1]) - firstVertex;
                int c = checked((int)Indices[i + 2]) - firstVertex;
                Vector3 face = Vector3.Cross(
                    Vertices[firstVertex + b].Position - Vertices[firstVertex + a].Position,
                    Vertices[firstVertex + c].Position - Vertices[firstVertex + a].Position);
                sums[a] += face; sums[b] += face; sums[c] += face;
            }
            for (int i = 0; i < vertexCount; i++)
            {
                Vector3 normal = sums[i].LengthSquared() > 1e-20f ? Vector3.Normalize(sums[i]) : Vector3.UnitY;
                Vertices[firstVertex + i] = Vertices[firstVertex + i] with { Normal = normal };
            }
        }

        private Accessor GetAccessor(int index)
        {
            JsonElement accessors = _root.GetProperty("accessors");
            if ((uint)index >= (uint)accessors.GetArrayLength())
                throw new FormatException($"Accessor index {index} is out of range.");
            JsonElement accessor = accessors[index];
            if (accessor.TryGetProperty("sparse", out _))
                throw new NotSupportedException("Sparse glTF accessors are not supported.");
            int viewIndex = accessor.GetProperty("bufferView").GetInt32();
            JsonElement views = _root.GetProperty("bufferViews");
            if ((uint)viewIndex >= (uint)views.GetArrayLength())
                throw new FormatException($"Buffer view index {viewIndex} is out of range.");
            JsonElement view = views[viewIndex];
            if (view.TryGetProperty("buffer", out JsonElement buffer) && buffer.GetInt32() != 0)
                throw new NotSupportedException("A GLB accessor refers to an external buffer.");
            int componentType = accessor.GetProperty("componentType").GetInt32();
            string type = accessor.GetProperty("type").GetString() ?? throw new FormatException("Accessor type is null.");
            int count = accessor.GetProperty("count").GetInt32();
            int components = type switch { "SCALAR" => 1, "VEC2" => 2, "VEC3" => 3, "VEC4" => 4, _ => throw new NotSupportedException($"Accessor type {type} is not supported.") };
            int componentBytes = componentType switch { 5120 or 5121 => 1, 5122 or 5123 => 2, 5125 or 5126 => 4, _ => throw new NotSupportedException($"Accessor component type {componentType} is not supported.") };
            int elementSize = checked(components * componentBytes);
            int stride = view.TryGetProperty("byteStride", out JsonElement byteStride) ? byteStride.GetInt32() : elementSize;
            if (stride < elementSize)
                throw new FormatException("Accessor stride is smaller than its element.");
            int offset = (view.TryGetProperty("byteOffset", out JsonElement viewOffset) ? viewOffset.GetInt32() : 0)
                + (accessor.TryGetProperty("byteOffset", out JsonElement accessorOffset) ? accessorOffset.GetInt32() : 0);
            int viewLength = view.GetProperty("byteLength").GetInt32();
            int required = count == 0 ? 0 : checked((count - 1) * stride + elementSize);
            int localAccessorOffset = accessor.TryGetProperty("byteOffset", out JsonElement local) ? local.GetInt32() : 0;
            if (offset < 0 || required < 0 || localAccessorOffset > viewLength - required || offset > _binary.Length - required)
                throw new FormatException("Accessor data range is outside its buffer view.");
            bool normalized = accessor.TryGetProperty("normalized", out JsonElement norm) && norm.GetBoolean();
            return new Accessor(_binary, offset, stride, count, componentType, type, components, normalized);
        }

        private string TextureName(int textureIndex)
        {
            if (!_root.TryGetProperty("textures", out JsonElement textures) || (uint)textureIndex >= (uint)textures.GetArrayLength())
                throw new FormatException($"Texture index {textureIndex} is out of range.");
            JsonElement texture = textures[textureIndex];
            int sourceIndex = texture.GetProperty("source").GetInt32();
            JsonElement images = _root.GetProperty("images");
            if ((uint)sourceIndex >= (uint)images.GetArrayLength())
                throw new FormatException($"Image index {sourceIndex} is out of range.");
            JsonElement image = images[sourceIndex];
            if (image.TryGetProperty("uri", out JsonElement uri))
                return uri.GetString()?.Replace('\\', '/') ?? $"image_{sourceIndex}";
            if (image.TryGetProperty("name", out JsonElement name))
                return $"embedded:{name.GetString() ?? $"image_{sourceIndex}"}";
            return $"embedded:image_{sourceIndex}";
        }

        private static Vector2 ReadVector2(JsonElement source, string property, Vector2 fallback)
        {
            if (!source.TryGetProperty(property, out JsonElement value))
                return fallback;
            if (value.GetArrayLength() != 2)
                throw new FormatException($"KHR_texture_transform {property} must contain two numbers.");
            var result = new Vector2(value[0].GetSingle(), value[1].GetSingle());
            if (!float.IsFinite(result.X) || !float.IsFinite(result.Y))
                throw new FormatException($"KHR_texture_transform {property} must be finite.");
            return result;
        }

        private readonly record struct TextureTransform(Vector2 Offset, Vector2 Scale, float Rotation)
        {
            public static TextureTransform Identity { get; } = new(Vector2.Zero, Vector2.One, 0f);

            public Vector2 Apply(Vector2 value)
            {
                Vector2 scaled = value * Scale;
                float cosine = MathF.Cos(Rotation);
                float sine = MathF.Sin(Rotation);
                return new Vector2(
                    Offset.X + cosine * scaled.X - sine * scaled.Y,
                    Offset.Y + sine * scaled.X + cosine * scaled.Y);
            }
        }

        private static Matrix4x4 NodeMatrix(JsonElement node)
        {
            if (node.TryGetProperty("matrix", out JsonElement matrix))
            {
                if (matrix.GetArrayLength() != 16)
                    throw new FormatException("Node matrix must contain 16 numbers.");
                var m = new float[16];
                int i = 0;
                foreach (JsonElement value in matrix.EnumerateArray())
                    m[i++] = value.GetSingle();
                return new Matrix4x4(m[0], m[1], m[2], m[3], m[4], m[5], m[6], m[7],
                    m[8], m[9], m[10], m[11], m[12], m[13], m[14], m[15]);
            }
            Vector3 translation = Vector3.Zero;
            Quaternion rotation = Quaternion.Identity;
            Vector3 scale = Vector3.One;
            if (node.TryGetProperty("translation", out JsonElement t))
                translation = new Vector3(t[0].GetSingle(), t[1].GetSingle(), t[2].GetSingle());
            if (node.TryGetProperty("rotation", out JsonElement r))
            {
                var candidate = new Quaternion(r[0].GetSingle(), r[1].GetSingle(), r[2].GetSingle(), r[3].GetSingle());
                rotation = candidate.LengthSquared() > 1e-20f ? Quaternion.Normalize(candidate) : Quaternion.Identity;
            }
            if (node.TryGetProperty("scale", out JsonElement s))
                scale = new Vector3(s[0].GetSingle(), s[1].GetSingle(), s[2].GetSingle());
            return Matrix4x4.CreateScale(scale) * Matrix4x4.CreateFromQuaternion(rotation) * Matrix4x4.CreateTranslation(translation);
        }

        private static uint Pack(float r, float g, float b, float a)
        {
            static uint B(float value) => (uint)(Math.Clamp(value, 0f, 1f) * 255f + 0.5f);
            return (B(a) << 24) | (B(r) << 16) | (B(g) << 8) | B(b);
        }

        private static bool Unit(float value) => float.IsFinite(value) && value is >= 0f and <= 1f;
    }

    private sealed class Accessor
    {
        private readonly byte[] _data;
        private readonly int _offset;
        private readonly int _stride;
        private readonly int _components;
        private readonly bool _normalized;

        public Accessor(byte[] data, int offset, int stride, int count, int componentType, string type, int components, bool normalized)
        {
            _data = data; _offset = offset; _stride = stride; Count = count; ComponentType = componentType;
            Type = type; _components = components; _normalized = normalized;
        }

        public int Count { get; }
        public int ComponentType { get; }
        public string Type { get; }
        public bool Normalized => _normalized;

        public Vector4 Read(int index, float defaultW = 0)
        {
            if ((uint)index >= (uint)Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            Span<float> values = stackalloc float[4];
            values[3] = defaultW;
            int componentSize = ComponentType is 5120 or 5121 ? 1 : ComponentType is 5122 or 5123 ? 2 : 4;
            int start = _offset + index * _stride;
            for (int i = 0; i < _components; i++)
                values[i] = ReadComponent(_data.AsSpan(start + i * componentSize));
            return new Vector4(values[0], values[1], values[2], values[3]);
        }

        public uint ReadUnsigned(int index)
        {
            int start = _offset + index * _stride;
            return ComponentType switch
            {
                5121 => _data[start],
                5123 => BinaryPrimitives.ReadUInt16LittleEndian(_data.AsSpan(start)),
                5125 => BinaryPrimitives.ReadUInt32LittleEndian(_data.AsSpan(start)),
                _ => throw new NotSupportedException("Accessor is not an unsigned index accessor."),
            };
        }

        private float ReadComponent(ReadOnlySpan<byte> source)
            => ComponentType switch
            {
                5120 => _normalized ? Math.Max(source[0] >= 128 ? source[0] - 256 : source[0], -127) / 127f : (sbyte)source[0],
                5121 => _normalized ? source[0] / 255f : source[0],
                5122 => _normalized ? Math.Max(BinaryPrimitives.ReadInt16LittleEndian(source), (short)-32767) / 32767f : BinaryPrimitives.ReadInt16LittleEndian(source),
                5123 => _normalized ? BinaryPrimitives.ReadUInt16LittleEndian(source) / 65535f : BinaryPrimitives.ReadUInt16LittleEndian(source),
                5125 => BinaryPrimitives.ReadUInt32LittleEndian(source),
                5126 => BinaryPrimitives.ReadSingleLittleEndian(source),
                _ => throw new NotSupportedException($"Accessor component type {ComponentType} is not supported."),
            };
    }
}
