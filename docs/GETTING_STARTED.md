# Getting started

## 1. Place the SDK outside this repository

Extract either supplied SharpProspero archive. Their extracted content was verified identical after normalizing the different top-level directory names. Do not copy the SDK into MM ENGINE unless you have consciously selected compatible licensing and distribution terms.

Set the environment variable to the folder containing `build`, `src`, `tools`, and `doctor.ps1`:

```powershell
$env:SHARPPROSPERO_ROOT = 'D:\dev\SharpProspero-0.8'
./scripts/doctor.ps1
```

On Windows, the SDK's NativeAOT compile phase runs in WSL. Install .NET 10 both on Windows and in the chosen WSL distribution. On Linux, install .NET 10 directly.

## 2. Run portable tests

These projects do not depend on SharpProspero and are the fastest way to check the core runtime and asset codec:

```powershell
dotnet build MMEngine.Portable.slnx -c Release
dotnet run --project tests/MMEngine.Tests/MMEngine.Tests.csproj -c Release
```

## 3. Build one PS5 sample

Windows:

```bat
START-HERE.bat Hello2D
```

PowerShell on either host:

```powershell
./scripts/build-ps5.ps1 -ProjectPath ./samples/Hello2D/Hello2D.csproj -Output Package
```

The canonical SharpProspero stages are retained: NativeAOT publish to a fresh `.o`, runtime-pack discovery, `ProsperoLink`, required module gathering, metadata/system-version processing, ELF wrapping/signing, and folder/package creation. A host link error during NativeAOT publish is expected by the SDK; a fresh object file is the actual compile-stage gate. The SDK driver, not MM ENGINE, makes that determination.

## 4. Deploy and test

Install the package through your normal lawful homebrew workflow. Run the checks in `docs/HARDWARE_TEST_PLAN.md` in order. Capture the complete development-console output and a short video for every failure.

## New application checklist

1. Copy the closest sample folder and give it a new assembly name.
2. Replace the development `titleId` and `contentId` in `sce_sys/param.json` with identifiers appropriate to your environment.
3. Keep both `Prospero.App.props` and `Prospero.App.targets` imports.
4. Keep the direct SharpProspero project reference and the needed MM ENGINE project references.
5. Put package assets under the sample's `assets` folder and keep the `MMEngine.PackageAssets.targets` import. The post-link target stages them in `/app0/assets` before the stock packager runs; the stock driver otherwise gathers only `sce_sys` and `sce_module`.
6. End `Main` with `ProcessExit.Exit()` after the application loop returns.
7. Use `MmApplication2D` for native CPU framebuffer applications or `MmApplication3D` for GPU-owned AGC presentation. Do not combine their presentation paths.
