# Build and CLI workflows

## Build entry points

`scripts/doctor.ps1` checks .NET 10, the SDK root, and WSL on Windows. `scripts/build-ps5.ps1` calls the verified SDK driver with the exact `-ProjectPath` parameter, verifies `out/module/eboot.bin`, verifies a package when requested, and preserves the full transcript. `scripts/build-all-samples.ps1` applies that path to all four samples.

```powershell
./scripts/build-ps5.ps1 -ProjectPath ./samples/Hello3D/Hello3D.csproj -Output Package
./scripts/build-all-samples.ps1 -Output Folder
```

The build does not require the legacy backend ELF shown in the supplied error evidence. All native object and module inputs come from the current project's NativeAOT output, .NET runtime pack, SharpProspero linker inputs, and platform modules.

SharpProspero's stock driver gathers only `sce_sys` and `sce_module` from the project. Each sample therefore imports `build/MMEngine.PackageAssets.targets`; immediately after `ProsperoLink`, it copies the sample's `assets` tree into the module staging root so the packager installs it as `/app0/assets`.

## MM ENGINE CLI

Build the host CLI:

```powershell
dotnet build tools/MMEngine.Cli/MMEngine.Cli.csproj -c Release
```

Import or validate a mesh:

```powershell
dotnet run --project tools/MMEngine.Cli/MMEngine.Cli.csproj -- mesh import model.obj model.mmmesh
dotnet run --project tools/MMEngine.Cli/MMEngine.Cli.csproj -- mesh import model.glb model.mmmesh --scale 0.01 --flip-v
dotnet run --project tools/MMEngine.Cli/MMEngine.Cli.csproj -- mesh validate model.mmmesh
```

Validate a scene or invoke the PS5 wrapper:

```powershell
dotnet run --project tools/MMEngine.Cli/MMEngine.Cli.csproj -- scene validate examples/scenes/hierarchy.mmscene
dotnet run --project tools/MMEngine.Cli/MMEngine.Cli.csproj -- build ps5 samples/Hello2D/Hello2D.csproj
```

`mesh validate` decodes the file, performs structural/range checks, re-encodes it, and requires byte-for-byte canonical output.

`scene validate` checks the v1 document, transforms, hierarchy, and component record shape. It deliberately cannot validate application-specific component names or property schemas; that semantic gate belongs to the reflection-free `ISceneComponentFactory` implemented by the application.

## Texture conversion

The SDK's SharpProspero.Texture component is a library, not a command-line program. MM ENGINE supplies the thin `mmtexture` host front end:

```powershell
dotnet run --project tools/MMEngine.TextureConverter/MMEngine.TextureConverter.csproj -- input.png output.gnf --srgb
dotnet run --project tools/MMEngine.TextureConverter/MMEngine.TextureConverter.csproj -- input.tga output.gnf --resize 1024x1024 --flip-v
```

It accepts PNG, TGA, BMP, and QOI through the SDK decoders, writes a linear GNF, then reads the header back and verifies size/dimensions. This establishes deterministic texture preprocessing; it does not claim that the current 3D renderer samples the GNF.

## Failure reporting

When a build fails, attach:

- `artifacts/logs/build-<project>.log` in full;
- the exact SDK commit/archive hash;
- host OS, `dotnet --info`, and WSL `dotnet --info` when applicable;
- the first missing symbol or tool error, not only the final `Link failed` wrapper line.
