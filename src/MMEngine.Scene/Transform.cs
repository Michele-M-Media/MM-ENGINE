using System;
using System.Collections.Generic;
using System.Numerics;

namespace MMEngine.Scene;

/// <summary>Hierarchical local/world transform using System.Numerics row-vector matrix order.</summary>
public sealed class Transform
{
    private readonly List<Transform> _children = [];
    private Transform? _parent;
    private Vector3 _localPosition;
    private Quaternion _localRotation = Quaternion.Identity;
    private Vector3 _localScale = Vector3.One;
    private Matrix4x4 _worldMatrix = Matrix4x4.Identity;
    private bool _dirty = true;

    internal Transform(GameObject owner) => GameObject = owner;

    public GameObject GameObject { get; }
    public Transform? Parent => _parent;
    public IReadOnlyList<Transform> Children => _children;

    public Vector3 LocalPosition
    {
        get => _localPosition;
        set { _localPosition = value; MarkDirty(); }
    }

    public Quaternion LocalRotation
    {
        get => _localRotation;
        set
        {
            _localRotation = value.LengthSquared() > 0f ? Quaternion.Normalize(value) : Quaternion.Identity;
            MarkDirty();
        }
    }

    public Vector3 LocalScale
    {
        get => _localScale;
        set { _localScale = value; MarkDirty(); }
    }

    public Matrix4x4 LocalMatrix
        => Matrix4x4.CreateScale(_localScale)
         * Matrix4x4.CreateFromQuaternion(_localRotation)
         * Matrix4x4.CreateTranslation(_localPosition);

    public Matrix4x4 WorldMatrix
    {
        get
        {
            if (_dirty)
            {
                _worldMatrix = _parent is null ? LocalMatrix : LocalMatrix * _parent.WorldMatrix;
                _dirty = false;
            }
            return _worldMatrix;
        }
    }

    public Vector3 Position => WorldMatrix.Translation;

    public void SetLocal(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        _localPosition = position;
        _localRotation = rotation.LengthSquared() > 0f ? Quaternion.Normalize(rotation) : Quaternion.Identity;
        _localScale = scale;
        MarkDirty();
    }

    public void SetParent(Transform? parent, bool worldPositionStays = false)
    {
        if (ReferenceEquals(parent, this))
            throw new InvalidOperationException("A Transform cannot parent itself.");
        for (Transform? cursor = parent; cursor is not null; cursor = cursor.Parent)
            if (ReferenceEquals(cursor, this))
                throw new InvalidOperationException("Transform parenting would create a cycle.");
        if (ReferenceEquals(_parent, parent))
            return;

        Matrix4x4 oldWorld = WorldMatrix;
        Vector3 nextScale = _localScale;
        Quaternion nextRotation = _localRotation;
        Vector3 nextPosition = _localPosition;

        if (worldPositionStays)
        {
            Matrix4x4 local = oldWorld;
            if (parent is not null)
            {
                if (!Matrix4x4.Invert(parent.WorldMatrix, out Matrix4x4 inverseParent))
                    throw new InvalidOperationException("The new parent Transform is not invertible.");
                local = oldWorld * inverseParent;
            }
            if (!Matrix4x4.Decompose(local, out Vector3 scale, out Quaternion rotation, out Vector3 translation))
                throw new InvalidOperationException("The world transform cannot be decomposed under the new parent.");
            nextScale = scale;
            nextRotation = rotation;
            nextPosition = translation;
        }

        _parent?._children.Remove(this);
        _parent = parent;
        _parent?._children.Add(this);
        _localScale = nextScale;
        _localRotation = nextRotation;
        _localPosition = nextPosition;
        MarkDirty();
    }

    public void Translate(Vector3 delta) => LocalPosition += delta;

    public void Rotate(Quaternion delta) => LocalRotation = Quaternion.Normalize(LocalRotation * delta);

    public void RotateY(float radians) => Rotate(Quaternion.CreateFromAxisAngle(Vector3.UnitY, radians));

    private void MarkDirty()
    {
        if (_dirty)
            return;
        _dirty = true;
        for (int i = 0; i < _children.Count; i++)
            _children[i].MarkDirty();
    }
}
