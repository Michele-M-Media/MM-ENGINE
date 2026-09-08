# 🚀 MM ENGINE v0.1-alpha — First Public Alpha

**MM ENGINE is now public.**

This is the first source alpha of MM ENGINE: a compact, inspectable engine/runtime foundation aimed at native **PlayStation 5 homebrew development**.

The goal of this release is not to pretend the engine is finished. It is to publish a real, buildable base with clear boundaries, reproducible tooling, documented validation and room to grow.

> **Small core. Native focus. Evidence-driven development.**

## ✨ Highlights

- portable core/runtime services and timing;
- scene, GameObject, component lifecycle and transform hierarchy;
- reflection-free `.mmscene` loading;
- deterministic `.mmmesh` v1 format and asset registry;
- OBJ and GLB import/validation CLI;
- DualSense snapshot, edge, vibration and light-bar abstractions;
- SharpProspero-backed 2D drawing, images, scaling, alpha, nine-slice, text and UI;
- experimental AGC multi-mesh 3D using the SDK built-in shader path;
- GNF texture-conversion front end;
- four package-ready samples: **Hello2D**, **Hello3D**, **InputDemo** and **ModelViewer**;
- portable executable tests;
- build wrappers, SDK baseline checks, hardware test plan, troubleshooting and diagnostics documentation.

## 🧪 Validation

The public tree is continuously checked with the portable CI pipeline on .NET 10.

For this alpha, the validated portable path includes restore, Release build, executable tests and source-manifest verification.

Real PS5 staged probes also advanced through display, pad, engine canvas, platform 2D, module loading and asset reads. The remaining asset-path failure was narrowed to the TrueType font construction path (`TrueTypeFont.Load`).

That boundary is documented rather than hidden: **v0.1-alpha is an alpha**. 3D depth, sampled material textures, 3D alpha blending and mixed CPU/GPU tiled composition remain blocked or experimental until their exact paths are demonstrated on hardware.

See:

- `docs/VALIDATION_UPDATE_2026-09-08.md`
- `docs/HARDWARE_STATUS.md`
- `docs/HARDWARE_TEST_LOG.md`
- `docs/STATUS_MATRIX.md`

## 🛠️ Start here

If you want to inspect, build or extend MM ENGINE, begin with:

- `README.md`
- `docs/GETTING_STARTED.md`
- `docs/MM_ENGINE_COMPLETE_BUILD_PACKAGING_AND_DIAGNOSTICS_GUIDE.txt`
- `BUILDING.md`
- `docs/TROUBLESHOOTING.md`

The repository intentionally keeps the architecture readable and the validation state explicit so contributors can tell the difference between **implemented**, **portable-validated**, **hardware-tested**, **experimental** and **blocked** functionality.

## ❤️ Support MM ENGINE

MM ENGINE is developed as an open-source project and the repository is configured for **GitHub Sponsors** under `Michele-M-Media`.

Sponsorship can help fund continued engine development, PS5 test hardware, infrastructure, documentation and tooling.

Supporting the project does **not** change the GPL license or the project's validation standards.

## 📜 License

MM ENGINE is released under **GPL-3.0-or-later**.

Third-party notices and licenses are included in the repository. This public source release excludes development-only reference media, private test files, proprietary SDK archives, title credentials, console secrets, generated packages and other non-redistributable/local material.

MM ENGINE is independent community software and is **not affiliated with or endorsed by Sony Interactive Entertainment**.

---

### v0.1-alpha

This is the beginning of the public MM ENGINE journey. Expect rough edges, experiments and rapid iteration — but also reproducible builds, visible limits and a codebase intended to be understood, tested and improved.

**Build it. Test it. Break it. Improve it.**
