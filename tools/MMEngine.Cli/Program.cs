using System;
using System.Diagnostics;
using System.IO;
using MMEngine.Assets;
using MMEngine.Scene;

namespace MMEngine.Cli;

internal static class Program
{
    private static int Main(string[] args)
    {
        try
        {
            return Run(args);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"mmengine: {exception.Message}");
            return 1;
        }
    }

    private static int Run(string[] args)
    {
        if (args.Length == 0 || args[0] is "help" or "--help" or "-h")
        {
            Help();
            return 0;
        }
        if (args.Length >= 2 && args[0] == "mesh" && args[1] == "import")
            return ImportMesh(args);
        if (args.Length == 3 && args[0] == "mesh" && args[1] == "validate")
            return ValidateMesh(args[2]);
        if (args.Length == 3 && args[0] == "scene" && args[1] == "validate")
            return ValidateScene(args[2]);
        if (args.Length == 3 && args[0] == "build" && args[1] == "ps5")
            return BuildPs5(args[2]);
        throw new ArgumentException("Unknown or incomplete command. Run 'mmengine help'.");
    }

    private static int ImportMesh(string[] args)
    {
        if (args.Length < 4)
            throw new ArgumentException("Usage: mmengine mesh import <input.obj|input.glb> <output.mmmesh> [--scale N] [--flip-v].");
        float scale = 1f;
        bool flipV = false;
        for (int i = 4; i < args.Length; i++)
        {
            if (args[i] == "--flip-v")
                flipV = true;
            else if (args[i] == "--scale" && i + 1 < args.Length)
                scale = Parse.Float(args[++i], "scale");
            else
                throw new ArgumentException($"Unknown mesh-import option '{args[i]}'.");
        }
        string extension = Path.GetExtension(args[2]).ToLowerInvariant();
        ImportResult result = extension switch
        {
            ".obj" => ObjImporter.Import(args[2], scale, flipV),
            ".glb" => GlbImporter.Import(args[2], scale, flipV),
            _ => throw new NotSupportedException($"Unsupported source extension '{extension}'. Use OBJ or binary glTF (.glb)."),
        };
        File.WriteAllBytes(args[3], MmMeshCodec.Encode(result.Mesh));
        foreach (string warning in result.Warnings)
            Console.Error.WriteLine($"warning: {warning}");
        Console.WriteLine($"Wrote {args[3]}: {result.Mesh.Vertices.Length} vertices, {result.Mesh.Indices.Length / 3} triangles, {result.Mesh.SubMeshes.Length} submeshes, {result.Mesh.Materials.Length} materials.");
        return 0;
    }

    private static int ValidateMesh(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        MmMeshAsset mesh = MmMeshCodec.Decode(bytes);
        byte[] canonical = MmMeshCodec.Encode(mesh);
        if (!bytes.AsSpan().SequenceEqual(canonical))
            throw new InvalidOperationException("Mesh decodes, but is not in canonical .mmmesh v1 encoding.");
        Console.WriteLine($"OK .mmmesh v1: {mesh.Vertices.Length} vertices, {mesh.Indices.Length / 3} triangles, {mesh.SubMeshes.Length} submeshes.");
        return 0;
    }

    private static int ValidateScene(string path)
    {
        using MMEngine.Scene.Scene scene = MmSceneLoader.Load(File.ReadAllBytes(path), new StructuralValidationComponentFactory());
        Console.WriteLine($"OK .mmscene v1 structure: '{scene.Name}', {scene.GameObjects.Count} objects. Component types/properties require the application's factory for semantic validation.");
        return 0;
    }

    private static int BuildPs5(string project)
    {
        string root = FindRepositoryRoot(AppContext.BaseDirectory);
        string script = Path.Combine(root, "scripts", "build-ps5.ps1");
        string shell = OperatingSystem.IsWindows() ? "powershell" : "pwsh";
        var start = new ProcessStartInfo(shell)
        {
            UseShellExecute = false,
        };
        start.ArgumentList.Add("-NoProfile");
        start.ArgumentList.Add("-ExecutionPolicy");
        start.ArgumentList.Add("Bypass");
        start.ArgumentList.Add("-File");
        start.ArgumentList.Add(script);
        start.ArgumentList.Add("-ProjectPath");
        start.ArgumentList.Add(Path.GetFullPath(project));
        using Process process = Process.Start(start) ?? throw new InvalidOperationException("Could not start PowerShell.");
        process.WaitForExit();
        return process.ExitCode;
    }

    private static string FindRepositoryRoot(string start)
    {
        DirectoryInfo? directory = new(start);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "MMEngine.slnx")))
                return directory.FullName;
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate MMEngine.slnx above the CLI executable.");
    }

    private static void Help()
    {
        Console.WriteLine("MM ENGINE v0.1-alpha CLI");
        Console.WriteLine("  mmengine mesh import <input.obj|input.glb> <output.mmmesh> [--scale N] [--flip-v]");
        Console.WriteLine("  mmengine mesh validate <file.mmmesh>");
        Console.WriteLine("  mmengine scene validate <file.mmscene>");
        Console.WriteLine("  mmengine build ps5 <sample.csproj>");
    }

    private sealed class StructuralValidationComponentFactory : ISceneComponentFactory
    {
        public Component Create(string type, System.Text.Json.JsonElement properties) => new StructuralValidationComponent();
    }

    private sealed class StructuralValidationComponent : Component { }
}
