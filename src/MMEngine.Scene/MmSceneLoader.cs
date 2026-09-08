using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json;

namespace MMEngine.Scene;

public interface ISceneComponentFactory
{
    Component? Create(string type, JsonElement properties);
}

/// <summary>Reflection-free JSON loader for the text-friendly .mmscene v1 format.</summary>
public static class MmSceneLoader
{
    public static Scene Load(ReadOnlySpan<byte> utf8Json, ISceneComponentFactory? componentFactory = null)
    {
        using JsonDocument document = JsonDocument.Parse(utf8Json.ToArray());
        JsonElement root = document.RootElement;
        int version = RequiredInt(root, "version");
        if (version != 1)
            throw new InvalidOperationException($"Unsupported .mmscene version {version}; expected 1.");

        string sceneName = OptionalString(root, "name") ?? "Scene";
        var scene = new Scene(sceneName);
        var byId = new Dictionary<string, GameObject>(StringComparer.Ordinal);
        var parentIds = new Dictionary<GameObject, string>();

        if (!root.TryGetProperty("objects", out JsonElement objects) || objects.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("A .mmscene file needs an 'objects' array.");

        foreach (JsonElement item in objects.EnumerateArray())
        {
            string id = RequiredString(item, "id");
            if (byId.ContainsKey(id))
                throw new InvalidOperationException($"Duplicate scene object id '{id}'.");
            var gameObject = new GameObject(OptionalString(item, "name") ?? id)
            {
                Tag = OptionalString(item, "tag") ?? "Untagged",
                Layer = OptionalInt(item, "layer") ?? 0,
                ActiveSelf = OptionalBool(item, "active") ?? true,
            };
            scene.Add(gameObject);
            byId.Add(id, gameObject);

            if (item.TryGetProperty("transform", out JsonElement transform))
            {
                Vector3 position = Vector3Value(transform, "position", Vector3.Zero);
                Vector3 scale = Vector3Value(transform, "scale", Vector3.One);
                Quaternion rotation = QuaternionValue(transform, "rotation", Quaternion.Identity);
                gameObject.Transform.SetLocal(position, rotation, scale);
            }

            string? parent = OptionalString(item, "parent");
            if (parent is not null)
                parentIds.Add(gameObject, parent);

            if (item.TryGetProperty("components", out JsonElement components))
            {
                if (components.ValueKind != JsonValueKind.Array)
                    throw new InvalidOperationException($"Object '{id}' has a non-array components value.");
                foreach (JsonElement componentJson in components.EnumerateArray())
                {
                    string type = RequiredString(componentJson, "type");
                    JsonElement properties = default;
                    if (componentJson.TryGetProperty("properties", out JsonElement p))
                    {
                        if (p.ValueKind != JsonValueKind.Object)
                            throw new InvalidOperationException($"Component '{type}' on object '{id}' has non-object properties.");
                        properties = p;
                    }
                    Component? component = componentFactory?.Create(type, properties);
                    if (component is null)
                        throw new InvalidOperationException($"No component factory handled '{type}' on object '{id}'.");
                    gameObject.AddComponent(component);
                }
            }
        }

        foreach (KeyValuePair<GameObject, string> pair in parentIds)
        {
            if (!byId.TryGetValue(pair.Value, out GameObject? parent) || parent is null)
                throw new InvalidOperationException($"Unknown parent id '{pair.Value}' for '{pair.Key.Name}'.");
            pair.Key.Transform.SetParent(parent.Transform);
        }

        return scene;
    }

    private static string RequiredString(JsonElement value, string name)
    {
        string? result = OptionalString(value, name);
        if (string.IsNullOrWhiteSpace(result))
            throw new InvalidOperationException($"Missing or empty string property '{name}'.");
        return result;
    }

    private static string? OptionalString(JsonElement value, string name)
        => value.TryGetProperty(name, out JsonElement property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private static int RequiredInt(JsonElement value, string name)
        => OptionalInt(value, name) ?? throw new InvalidOperationException($"Missing integer property '{name}'.");

    private static int? OptionalInt(JsonElement value, string name)
        => value.TryGetProperty(name, out JsonElement property) && property.TryGetInt32(out int result) ? result : null;

    private static bool? OptionalBool(JsonElement value, string name)
        => value.TryGetProperty(name, out JsonElement property) && property.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? property.GetBoolean()
            : null;

    private static Vector3 Vector3Value(JsonElement value, string name, Vector3 fallback)
    {
        if (!value.TryGetProperty(name, out JsonElement property))
            return fallback;
        if (property.ValueKind != JsonValueKind.Array || property.GetArrayLength() != 3)
            throw new InvalidOperationException($"Transform '{name}' must be a three-number array.");
        float[] parts = new float[3];
        int i = 0;
        foreach (JsonElement item in property.EnumerateArray())
            parts[i++] = item.GetSingle();
        return new Vector3(parts[0], parts[1], parts[2]);
    }

    private static Quaternion QuaternionValue(JsonElement value, string name, Quaternion fallback)
    {
        if (!value.TryGetProperty(name, out JsonElement property))
            return fallback;
        if (property.ValueKind != JsonValueKind.Array || property.GetArrayLength() != 4)
            throw new InvalidOperationException($"Transform '{name}' must be a four-number quaternion array [x,y,z,w].");
        float[] parts = new float[4];
        int i = 0;
        foreach (JsonElement item in property.EnumerateArray())
            parts[i++] = item.GetSingle();
        var result = new Quaternion(parts[0], parts[1], parts[2], parts[3]);
        return result.LengthSquared() > 0f ? Quaternion.Normalize(result) : Quaternion.Identity;
    }
}
