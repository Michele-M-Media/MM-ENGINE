using System;

namespace MMEngine.Assets;

/// <summary>A normalized, case-sensitive logical asset path.</summary>
public readonly record struct AssetId
{
    public AssetId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        string normalized = value.Replace('\\', '/').TrimStart('/');
        string[] segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < segments.Length; i++)
            if (segments[i] == "..")
                throw new ArgumentException("Asset identifiers cannot escape the asset root.", nameof(value));
        normalized = string.Join("/", segments);
        if (normalized.Length == 0)
            throw new ArgumentException("Asset identifiers cannot be empty.", nameof(value));
        Value = normalized;
    }

    public string Value { get; }
    public override string ToString() => Value;
}
