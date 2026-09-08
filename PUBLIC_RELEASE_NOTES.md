# MM ENGINE v0.1-alpha — first public alpha

MM ENGINE v0.1-alpha is the first public source alpha of the project.

This release focuses on a small, inspectable engine/runtime foundation for native PS5 homebrew development rather than a large editor-centric stack.

## Included

- portable core/runtime services and timing;
- scene/GameObject/component lifecycle and transform hierarchy;
- reflection-free `.mmscene` loading;
- deterministic `.mmmesh` v1 codec and asset registry;
- OBJ and GLB import/validation CLI;
- DualSense snapshot, edge, vibration, and light-bar abstractions;
- SharpProspero-backed 2D drawing, images, scaling, alpha, nine-slice, text, and UI;
- experimental AGC multi-mesh 3D using the SDK's built-in shader path;
- GNF texture conversion front end;
- four package-ready samples: Hello2D, Hello3D, InputDemo, ModelViewer;
- portable executable tests;
- build wrappers, SDK baseline checks, hardware plan, troubleshooting, and diagnostics documentation.

## Validation update

The original repository was assembled under static-validation constraints. Subsequent maintainer testing exercised more of the pipeline and real hardware.

The normal application path reached real PS5 display/frame execution in staged probes. The useful staged boundary advanced through display, pad, engine canvas, platform 2D, module loading and asset reads. The remaining asset-path failure was isolated more narrowly to the TrueType font construction path (`TrueTypeFont.Load`).

This does **not** make every renderer capability hardware-verified. The repository deliberately keeps 3D depth, sampled material textures, 3D alpha blending, and mixed CPU/GPU tiled composition marked blocked or experimental until their exact paths are demonstrated.

See `docs/VALIDATION_UPDATE_2026-09-08.md`, `docs/HARDWARE_STATUS.md`, `docs/HARDWARE_TEST_LOG.md`, and `docs/STATUS_MATRIX.md`.

## License and funding

MM ENGINE is released under GPL-3.0-or-later. The repository is also configured for GitHub Sponsors under `Michele-M-Media` to help fund development, test hardware, infrastructure, documentation, and tooling.

Sponsorship does not alter the open-source license or the project's evidence/validation standards.

## Redistribution boundary

This source release excludes development-only controller models/reference media, private test files, proprietary SDK archives, title credentials, console secrets, `bin/`, `obj/`, `out/`, `artifacts/`, generated packages, and other non-redistributable/local material.

MM ENGINE is independent community software and is not affiliated with or endorsed by Sony Interactive Entertainment.
