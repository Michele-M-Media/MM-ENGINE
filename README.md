# MM ENGINE v0.1-alpha

MM ENGINE is a code-first C# engine bootstrap for native PlayStation 5 homebrew applications built with SharpProspero 0.8 and .NET 10 NativeAOT. It is an independent, CLI-first repository: runtime, text scene files, asset pipeline, builds, and tests do not require a Windows GUI/editor. The repository contains a portable runtime, a PS5 platform backend, deterministic asset tools, four installable sample projects, host tests, and hardware test instructions.

The alpha is deliberately honest about the supplied SDK boundary. Native 2D drawing, alpha composition, image scaling, nine-slice panels, antialiased TrueType text, UI, DualSense input, built-in-shader 3D, and multi-mesh submission have source-backed implementations. 3D depth buffering and sampled material textures are not claimed: the SDK sources supplied to this task do not expose enough verified setup to implement them without guessing register state.

## Start here

Requirements:

- the .NET 10 SDK;
- the supplied unmodified SharpProspero 0.8 checkout;
- PowerShell 5.1+ or `pwsh`;
- WSL with .NET 10 when building from Windows, because NativeAOT emits the device object on Linux;
- a lawful PS5 homebrew test environment.

Set `SHARPPROSPERO_ROOT` to the SDK root, then on Windows run:

```bat
START-HERE.bat Hello2D
```

Valid sample names are `Hello2D`, `Hello3D`, `InputDemo`, and `ModelViewer`. The build uses SharpProspero's canonical `build/build-app.ps1 -ProjectPath ...` pipeline and writes the module/package below the selected sample's `out` directory. Complete transcripts are retained under `artifacts/logs`.

For portable host tests:

```powershell
dotnet run --project tests/MMEngine.Tests/MMEngine.Tests.csproj -c Release
```

For all sample packages:

```powershell
./scripts/build-all-samples.ps1 -Output Package
```

See [Getting started](docs/GETTING_STARTED.md), [BUILDING.md](BUILDING.md), [ARCHITECTURE.md](ARCHITECTURE.md), the [API overview](docs/API_OVERVIEW.md), and the [hardware test plan](docs/HARDWARE_TEST_PLAN.md).

## Status at handoff

| Area | State |
|---|---|
| Portable runtime, scenes, assets, input, animation, CLI | Implemented; workstation build/tests passed |
| Native 2D, font, UI, DualSense adapter | Implemented from source-verified SDK APIs; hardware validation incomplete |
| Real AGC multi-mesh 3D + color fallback | Experimental/source-verified; hardware gate pending |
| NativeAOT compile/link/sign | Exercised successfully for Hello2D; external Package step recorded a failure |
| 3D depth, sampled textures/blending, mixed CPU/GPU frame | Blocked on the verified SDK/toolchain gaps documented below |
| Hardware execution | Launch reached PS5 title/module path; runtime still experimental and CE-108255-1 remains under diagnosis |

Full per-capability evidence and release gates are in [the status matrix](docs/STATUS_MATRIX.md). The implementation sequence is in [ROADMAP.md](ROADMAP.md).

## Repository map

| Path | Purpose |
|---|---|
| `src/MMEngine.Core` | timing, diagnostics, services, logging |
| `src/MMEngine.Scene` | scenes, GameObjects, components, lifecycle, transform hierarchy, `.mmscene` |
| `src/MMEngine.Assets` | asset IDs/registry and deterministic `.mmmesh` v1 |
| `src/MMEngine.Input` | platform-neutral DualSense snapshots and frame edges |
| `src/MMEngine.Graphics2D` | SharpProspero Surface, PNG/TGA, scaling, blending, text |
| `src/MMEngine.Graphics3D` | AGC multi-draw renderer, GPU mesh upload, camera/mesh components |
| `src/MMEngine.UI` | immediate retained UI primitives: panels, cards, labels, images, buttons, sliders |
| `src/MMEngine.Platform.PS5` | application loops, modules, package I/O, logging, input adapter |
| `tools` | `mmengine` mesh/scene/build CLI and `mmtexture` GNF converter |
| `samples` | four PS5 application/package projects |
| `tests` | dependency-free portable executable test suite |

## Samples

| Sample | What it proves |
|---|---|
| Hello2D | native framebuffer drawing, PNG alpha, scaling, TrueType AA text, UI |
| Hello3D | one AGC frame containing several independently transformed mesh draws |
| InputDemo | connected state, edges, sticks/triggers, vibration and light-bar output |
| ModelViewer | deterministic `.mmmesh` load, multiple submeshes/material fallbacks, right-stick orbit controls |

The bundled ModelViewer fixture is original to this repository. The DualSense model from the task input is not redistributed; its materials and topology were used only as local design reference. Instructions for converting a model you are licensed to use are in [the asset pipeline](docs/ASSET_PIPELINE.md).

The diagnostic lineage remains explicit: MM API Mapper informs MM PS5 CONTROL, which in turn informs MM ENGINE. This bootstrap owns engine/frame/capability diagnostics; it does not copy the control application or fabricate console-health APIs that the supplied SDK does not verify.


## Public alpha resources

- [First public alpha release notes](PUBLIC_RELEASE_NOTES.md)
- [Complete build and diagnostics guide](docs/MM_ENGINE_GUIDA_COMPLETA_COMPILAZIONE_E_ERRORI.txt)
- [Validation update — 2026-09-08](docs/VALIDATION_UPDATE_2026-09-08.md)
- [Roadmap](ROADMAP.md)
- [Contributing](CONTRIBUTING.md)
- [Support / Sponsors](SPONSORS.md)

## Support development

MM ENGINE remains open source. If the GitHub Sponsors profile for `Michele-M-Media` is active, the repository Sponsor button can be used to help fund development, testing hardware, infrastructure, documentation and tooling. See [SPONSORS.md](SPONSORS.md).

## Alpha status

The repository was originally assembled in a static-validation environment. Subsequent maintainer testing passed the portable build/tests and exercised NativeAOT compile/link/module/metadata/system-version/sign for Hello2D, while the external package step failed and PS5 runtime validation remains incomplete. See [the validation update](docs/VALIDATION_UPDATE_2026-09-08.md), [STATUS_MATRIX.md](docs/STATUS_MATRIX.md), and [VALIDATION.md](docs/VALIDATION.md).

MM ENGINE v0.1-alpha is released under **GPL-3.0-or-later**. Read [LICENSE](LICENSE), [LICENSE-NOTICE.md](LICENSE-NOTICE.md), and [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) before redistribution. MM ENGINE is an independent community project and is not affiliated with Sony Interactive Entertainment.
