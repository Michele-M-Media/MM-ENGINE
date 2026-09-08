using System;
using System.Collections.Generic;

namespace MMEngine.Core;

/// <summary>Small explicit dependency registry; services are registered before the run loop.</summary>
public sealed class ServiceRegistry
{
    private readonly Dictionary<Type, object> _services = [];

    public void Add<T>(T service) where T : class
    {
        ArgumentNullException.ThrowIfNull(service);
        _services[typeof(T)] = service;
    }

    public bool TryGet<T>(out T? service) where T : class
    {
        if (_services.TryGetValue(typeof(T), out object? value) && value is T typed)
        {
            service = typed;
            return true;
        }

        service = null;
        return false;
    }

    public T Require<T>() where T : class
        => TryGet<T>(out T? service)
            ? service!
            : throw new InvalidOperationException($"Engine service '{typeof(T).FullName}' is not registered.");

    public void Clear() => _services.Clear();
}
