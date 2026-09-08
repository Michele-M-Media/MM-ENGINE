# Building MM ENGINE

## Portable build and tests

Install the .NET 10 SDK, then run from the repository root:

```powershell
dotnet build MMEngine.Portable.slnx -c Release
dotnet run --project tests/MMEngine.Tests/MMEngine.Tests.csproj -c Release
./scripts/build-assets.ps1
```

These commands cover the engine core, scene/component lifecycle, input state, animation, deterministic mesh codec, OBJ/GLB CLI, and original ModelViewer fixture without PS5 hardware.

## Native PS5 build

Extract one of the two supplied, source-identical SharpProspero 0.8 archives and set `SHARPPROSPERO_ROOT` to its root. On Windows, install .NET 10 inside WSL as well as on the host.

```powershell
$env:SHARPPROSPERO_ROOT = 'D:\dev\SharpProspero-0.8'
./scripts/doctor.ps1
./scripts/build-ps5.ps1 -ProjectPath ./samples/Hello2D/Hello2D.csproj -Output Package
./scripts/build-all-samples.ps1 -Output Package
```

The wrapper invokes the supplied canonical `build/build-app.ps1 -ProjectPath` pipeline, stages each sample's `assets` into `/app0/assets`, verifies `out/module/eboot.bin` and the requested package, and retains a complete transcript under `artifacts/logs`.

See [docs/BUILD_AND_CLI.md](docs/BUILD_AND_CLI.md) for CLI and texture commands, [docs/VALIDATION.md](docs/VALIDATION.md) for what has and has not run, and [docs/HARDWARE_TEST_PLAN.md](docs/HARDWARE_TEST_PLAN.md) for the console gate.

