using System;
using System.Collections.Generic;

namespace MMEngine.Assets;

/// <summary>Small deterministic registry; loading policy stays with the application.</summary>
public sealed class AssetRegistry
{
    private readonly Dictionary<AssetId, object> _assets = [];

    public int Count => _assets.Count;

    public void Add<T>(AssetId id, T asset) where T : class
    {
        ArgumentNullException.ThrowIfNull(asset);
        if (!_assets.TryAdd(id, asset))
            throw new InvalidOperationException($"Asset '{id}' is already registered.");
    }

    public T Get<T>(AssetId id) where T : class
    {
        if (!_assets.TryGetValue(id, out object? value))
            throw new KeyNotFoundException($"Asset '{id}' is not registered.");
        if (value is not T typed)
            throw new InvalidOperationException($"Asset '{id}' is {value?.GetType().Name ?? "null"}, not {typeof(T).Name}.");
        return typed;
    }

    public bool TryGet<T>(AssetId id, out T? asset) where T : class
    {
        asset = _assets.TryGetValue(id, out object? value) ? value as T : null;
        return asset is not null;
    }

    public bool Remove(AssetId id) => _assets.Remove(id);
    public void Clear() => _assets.Clear();
}
