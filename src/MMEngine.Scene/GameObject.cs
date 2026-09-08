using System;
using System.Collections.Generic;

namespace MMEngine.Scene;

/// <summary>Named scene object containing a Transform and zero or more Components.</summary>
public sealed class GameObject
{
    private readonly List<Component> _components = [];

    public GameObject(string name = "GameObject")
    {
        Name = string.IsNullOrWhiteSpace(name) ? "GameObject" : name;
        Transform = new Transform(this);
    }

    public string Name { get; set; }
    public string Tag { get; set; } = "Untagged";
    public int Layer { get; set; }
    public bool ActiveSelf { get; set; } = true;
    public bool ActiveInHierarchy => ActiveSelf && (Transform.Parent?.GameObject.ActiveInHierarchy ?? true);
    public Transform Transform { get; }
    public Scene? Scene { get; internal set; }
    public IReadOnlyList<Component> Components => _components;

    public T AddComponent<T>() where T : Component, new() => (T)AddComponent(new T());

    public Component AddComponent(Component component)
    {
        ArgumentNullException.ThrowIfNull(component);
        component.Attach(this);
        _components.Add(component);
        return component;
    }

    public T? GetComponent<T>() where T : Component
    {
        for (int i = 0; i < _components.Count; i++)
            if (_components[i] is T value && !value.IsDestroyed)
                return value;
        return null;
    }

    public bool TryGetComponent<T>(out T? component) where T : Component
    {
        component = GetComponent<T>();
        return component is not null;
    }

    public bool RemoveComponent(Component component)
    {
        if (!_components.Remove(component))
            return false;
        component.Destroy();
        return true;
    }

    internal void DestroyComponents()
    {
        for (int i = _components.Count - 1; i >= 0; i--)
            _components[i].Destroy();
        _components.Clear();
    }
}
