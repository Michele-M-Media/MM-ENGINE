# MM ENGINE roadmap

Every phase is evidence-gated. A capability moves to “working on PS5” only after a recorded build and hardware run; missing AGC state is never filled with guessed registers.

## v0.1-alpha — bootstrap

- Portable runtime, text scenes, GameObject/component/Transform lifecycle, assets, input, animation, and CLI.
- Native asset-driven 2D with PNG/TGA, alpha/scaling, nine-slice, TrueType AA, and custom UI controls.
- Real AGC multi-mesh submission using the supplied built-in shader pair, camera/model transforms, and vertex/material-color fallback.
- Four package projects, deterministic original fixture, loud build wrappers, source audit, status matrix, and hardware plan.

Exit gate: compile portable projects, build all four PS5 packages through the stock SharpProspero pipeline, then complete the hardware plan.

## v0.2 — verified depth and material shader path

Depth work starts only when a source-backed depth-target defaults block plus clear/bind/order sequence is available. The sampled-texture path requires all of the following:

1. a redistributable, reproducible PSSL shader compiler/toolchain or traceable precompiled `.sb` source artifact;
2. a pixel shader declaring base-color texture and sampler resources;
3. resource-slot reflection plus a verified pixel-stage user-data base/binder (the supplied stack exposes only the vertex/geometry path used today);
4. GPU texture allocation/upload from validated GNF, with descriptor lifetime owned per frame in flight;
5. a hardware sample proving UV orientation, sampling, sRGB behavior, depth, and material alpha independently.

Until those five gates pass, `.mmmesh` keeps UV/material/texture metadata plus a reserved sampler word, and `GpuMesh3D` deliberately renders the color fallback.

## v0.3 — composition and tooling

- Verify a supported CPU UI + tiled AGC composition path, or implement a GPU UI renderer; keep 2D/3D frame owners separate until then.
- Add project/schema validation, richer deterministic import fixtures, focus/navigation, layout, and material/UI tween helpers.
- Integrate capability/health results supplied by the MM API Mapper/MM PS5 CONTROL lineage through explicit interfaces, without embedding application-specific code in the engine.

## Public release gate

- Owner-selected project license compatible with the SharpProspero-derived GPL-covered renderer sequence.
- No local-only DualSense model, proprietary SDK/tool, title credential, build output, or console secret.
- Published archive/commit hashes, complete build logs, and hardware-tested status tied to the exact revision.


## Funding and continuation

GitHub Sponsors support is configured for `Michele-M-Media`. Sponsorship is intended to help fund continued engine work, test hardware, infrastructure, documentation and tooling; it does not change the GPL-3.0-or-later license or validation requirements.
