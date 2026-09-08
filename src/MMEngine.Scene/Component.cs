using System;
using MMEngine.Core;

namespace MMEngine.Scene;

/// <summary>Base unit of behavior attached to a <see cref="GameObject"/>.</summary>
public abstract class Component
{
    private GameObject? _gameObject;
    private bool _awoken;
    private bool _started;
    private bool _destroyed;

    public GameObject GameObject => _gameObject ?? throw new InvalidOperationException("The component is not attached.");
    public Transform Transform => GameObject.Transform;
    public bool Enabled { get; set; } = true;
    public bool IsDestroyed => _destroyed;

    internal void Attach(GameObject owner)
    {
        if (_gameObject is not null)
            throw new InvalidOperationException("A component can belong to only one GameObject.");
        _gameObject = owner;
    }

    internal void InvokeUpdate(in FrameTime time, EngineDiagnostics diagnostics)
    {
        if (_destroyed || !Enabled || !GameObject.ActiveInHierarchy)
            return;
        if (!_awoken)
        {
            _awoken = true;
            Awake();
        }
        if (!_started)
        {
            _started = true;
            Start();
        }
        Update(time);
        diagnostics.UpdatedComponents++;
    }

    internal void InvokeLateUpdate(in FrameTime time)
    {
        if (!_destroyed && _started && Enabled && GameObject.ActiveInHierarchy)
            LateUpdate(time);
    }

    internal void Destroy()
    {
        if (_destroyed)
            return;
        _destroyed = true;
        OnDestroy();
    }

    protected virtual void Awake() { }
    protected virtual void Start() { }
    protected virtual void Update(in FrameTime time) { }
    protected virtual void LateUpdate(in FrameTime time) { }
    protected virtual void OnDestroy() { }
}

/// <summary>Convenience base for user-authored per-frame components.</summary>
public abstract class MMBehaviour : Component
{
}
