using System;
using System.Collections.Generic;
using MMEngine.Core;

namespace MMEngine.Scene;

/// <summary>A reproducible collection of GameObjects with component lifecycle dispatch.</summary>
public sealed class Scene : IDisposable
{
    private readonly List<GameObject> _objects = [];
    private bool _disposed;

    public Scene(string name = "Scene") => Name = name;

    public string Name { get; set; }
    public IReadOnlyList<GameObject> GameObjects => _objects;

    public GameObject CreateGameObject(string name = "GameObject")
    {
        var value = new GameObject(name);
        Add(value);
        return value;
    }

    public void Add(GameObject gameObject)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(gameObject);
        if (gameObject.Scene is not null)
            throw new InvalidOperationException("The GameObject already belongs to a Scene.");
        gameObject.Scene = this;
        _objects.Add(gameObject);
    }

    public bool Remove(GameObject gameObject)
    {
        if (!_objects.Remove(gameObject))
            return false;
        gameObject.Transform.SetParent(null, worldPositionStays: true);
        while (gameObject.Transform.Children.Count > 0)
            gameObject.Transform.Children[0].SetParent(null, worldPositionStays: true);
        gameObject.DestroyComponents();
        gameObject.Scene = null;
        return true;
    }

    public GameObject? Find(string name)
    {
        for (int i = 0; i < _objects.Count; i++)
            if (string.Equals(_objects[i].Name, name, StringComparison.Ordinal))
                return _objects[i];
        return null;
    }

    public IEnumerable<T> GetComponents<T>() where T : class
    {
        for (int i = 0; i < _objects.Count; i++)
        {
            IReadOnlyList<Component> components = _objects[i].Components;
            for (int j = 0; j < components.Count; j++)
                if (!components[j].IsDestroyed && components[j] is T value)
                    yield return value;
        }
    }

    public void Update(in FrameTime time, EngineDiagnostics diagnostics)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(diagnostics);
        GameObject[] objects = _objects.ToArray();
        for (int i = 0; i < objects.Length; i++)
        {
            GameObject gameObject = objects[i];
            if (!gameObject.ActiveInHierarchy)
                continue;
            diagnostics.ActiveGameObjects++;
            Component[] components = new Component[gameObject.Components.Count];
            for (int j = 0; j < components.Length; j++)
                components[j] = gameObject.Components[j];
            for (int j = 0; j < components.Length; j++)
                components[j].InvokeUpdate(time, diagnostics);
        }

        for (int i = 0; i < objects.Length; i++)
        {
            IReadOnlyList<Component> components = objects[i].Components;
            for (int j = 0; j < components.Count; j++)
                components[j].InvokeLateUpdate(time);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        for (int i = _objects.Count - 1; i >= 0; i--)
        {
            _objects[i].DestroyComponents();
            _objects[i].Scene = null;
        }
        _objects.Clear();
    }
}
