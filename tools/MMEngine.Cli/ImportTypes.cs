using System;
using System.Collections.Generic;
using System.Globalization;
using MMEngine.Assets;

namespace MMEngine.Cli;

internal sealed record ImportResult(MmMeshAsset Mesh, IReadOnlyList<string> Warnings);

internal static class Parse
{
    public static float Float(string value, string field)
    {
        if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float result) || !float.IsFinite(result))
            throw new FormatException($"Invalid {field} value '{value}'.");
        return result;
    }

    public static int Int(string value, string field)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
            throw new FormatException($"Invalid {field} value '{value}'.");
        return result;
    }
}
