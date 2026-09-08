# Changelog

## 0.1.0-alpha - first public source alpha

- Added portable engine clock, diagnostics, services, scene/GameObject/component lifecycle, hierarchy transforms, and reflection-free text scenes.
- Added deterministic `.mmmesh` v1 codec plus OBJ and GLB conversion/validation CLI, including baked texture-transform UV support and explicit unsupported-material warnings.
- Added stable DualSense snapshot, pressed/held/released, vibration, and light-bar APIs.
- Added native SharpProspero 2D image, alpha, scaling, nine-slice, TrueType/bitmap text, and UI layers.
- Added experimental AGC multi-draw 3D renderer using the SDK's embedded shader pair and compact per-submesh GPU buffers.
- Added a host GNF conversion front end, four package-ready samples, portable tests, build wrappers, SDK baseline audit, and hardware plan.
- Subsequent maintainer validation crossed the normal display/pad/canvas/platform-2D path on real PS5 hardware and narrowed the remaining asset-path crash to TrueType font construction.
- Added public release notes, validation/hardware status, troubleshooting, complete build/packaging/diagnostics documentation, security policy, GitHub Sponsors configuration, and portable CI.
- Selected GPL-3.0-or-later for the MM ENGINE public source release.
- Kept depth, 3D texture sampling/blending, and tiled mixed CPU/GPU composition explicitly blocked pending verified support.
