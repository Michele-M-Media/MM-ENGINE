using System;
using System.Collections.Generic;
using System.Numerics;
using MMEngine.Scene;

namespace MMEngine.Graphics3D;

public sealed class Camera3DComponent : Component
{
    public float FieldOfViewRadians { get; set; } = MathF.PI / 3f;
    public float NearPlane { get; set; } = 0.05f;
    public float FarPlane { get; set; } = 500f;

    public Matrix4x4 View
    {
        get
        {
            if (!Matrix4x4.Invert(Transform.WorldMatrix, out Matrix4x4 view))
                throw new InvalidOperationException("Camera transform is not invertible.");
            return view;
        }
    }

    public Matrix4x4 ViewProjection(float aspect)
    {
        if (!float.IsFinite(aspect) || aspect <= 0)
            throw new ArgumentOutOfRangeException(nameof(aspect));
        if (NearPlane <= 0 || FarPlane <= NearPlane)
            throw new InvalidOperationException("Camera clip planes are invalid.");
        Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(FieldOfViewRadians, aspect, NearPlane, FarPlane);
        return View * projection;
    }
}

public sealed class MeshRenderer3D : Component
{
    public GpuMesh3D? Mesh { get; set; }
    public bool Visible { get; set; } = true;
    public int Order { get; set; }
}

public sealed class Scene3DRenderer
{
    private readonly MultiMeshRenderer3D _renderer;
    private readonly List<MeshRenderer3D> _items = [];

    public Scene3DRenderer(MultiMeshRenderer3D renderer)
        => _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));

    public void Render(MMEngine.Scene.Scene scene, Camera3DComponent camera, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(scene);
        ArgumentNullException.ThrowIfNull(camera);
        _items.Clear();
        foreach (MeshRenderer3D item in scene.GetComponents<MeshRenderer3D>())
            if (item.Enabled && item.Visible && item.Mesh is not null)
                _items.Add(item);
        _items.Sort(static (a, b) => a.Order.CompareTo(b.Order));

        Matrix4x4 viewProjection = camera.ViewProjection((float)width / height);
        _renderer.BeginFrame();
        for (int i = 0; i < _items.Count; i++)
        {
            MeshRenderer3D item = _items[i];
            _renderer.Draw(item.Mesh!, item.Transform.WorldMatrix, viewProjection);
        }
        _renderer.EndFrame();
    }
}
