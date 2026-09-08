using System;
using System.Collections.Generic;
using MMEngine.Assets;
using MMEngine.Core;
using SharpProspero.Graphics;
using SharpProspero.Graphics.Agc;

namespace MMEngine.Graphics3D;

/// <summary>One GPU buffer per source submesh, retaining its material for inspection and fallback.</summary>
public sealed class GpuMesh3D : IDisposable
{
    private bool _disposed;

    private GpuMesh3D(MmMeshAsset source, MeshBuffer[] buffers)
    {
        Source = source;
        SubMeshBuffers = buffers;
    }

    public MmMeshAsset Source { get; }
    public MeshBuffer[] SubMeshBuffers { get; }
    public int SubMeshCount => SubMeshBuffers.Length;

    public static GpuMesh3D Upload(MmMeshAsset mesh)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        var buffers = new MeshBuffer[mesh.SubMeshes.Length];
        try
        {
            for (int part = 0; part < mesh.SubMeshes.Length; part++)
            {
                MmSubMesh subMesh = mesh.SubMeshes[part];
                MmMaterial material = mesh.Materials[subMesh.MaterialIndex];
                var vertices = new List<Vertex>();
                var remap = new Dictionary<uint, uint>();
                var indices = new uint[subMesh.IndexCount];
                for (int i = 0; i < subMesh.IndexCount; i++)
                {
                    uint sourceIndex = mesh.Indices[subMesh.FirstIndex + i];
                    if (!remap.TryGetValue(sourceIndex, out uint compactIndex))
                    {
                        compactIndex = checked((uint)vertices.Count);
                        remap.Add(sourceIndex, compactIndex);
                        MmVertex source = mesh.Vertices[checked((int)sourceIndex)];
                        uint color = MultiplyArgb(source.ColorArgb, material.BaseColorArgb);
                        vertices.Add(new Vertex(source.Position, source.Normal, source.TexCoord, new Color(color)));
                    }
                    indices[i] = compactIndex;
                }
                buffers[part] = MeshBuffer.Upload(new MeshData([.. vertices], indices));
                if (material.BaseColorTexture is not null)
                    EngineLog.Warning($"Material '{material.Name}' texture '{material.BaseColorTexture}' retained but rendered with vertex/base color fallback: {Graphics3DCapabilities.TextureSamplingBlocker}");
            }
            return new GpuMesh3D(mesh, buffers);
        }
        catch
        {
            for (int i = 0; i < buffers.Length; i++)
                buffers[i]?.Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        for (int i = 0; i < SubMeshBuffers.Length; i++)
            SubMeshBuffers[i].Dispose();
    }

    private static uint MultiplyArgb(uint left, uint right)
    {
        uint Channel(int shift)
        {
            uint a = (left >> shift) & 0xFF;
            uint b = (right >> shift) & 0xFF;
            return ((a * b + 127) / 255) << shift;
        }
        return Channel(24) | Channel(16) | Channel(8) | Channel(0);
    }
}
