# Public alpha release checklist

This checklist distinguishes **source-publication readiness** from **feature-complete hardware validation**.

## Source-publication checks

- [x] Project-wide license selected: GPL-3.0-or-later.
- [x] SharpProspero/third-party redistribution boundary documented.
- [x] Development-only controller geometry, reference media, private SDK archives, credentials, and console secrets excluded.
- [x] Clean source tree excludes `bin`, `obj`, `out`, `artifacts`, IDE state, and generated packages.
- [x] Public README documents experimental/blocked capabilities.
- [x] Sponsorship configuration and support notice included.
- [x] Security and contribution guidance included.
- [x] Portable CI included.
- [x] Source manifest regenerated for the exact public tree.

## Validation state carried into the alpha

- [x] Portable build/tests exercised by maintainer.
- [x] Normal application NativeAOT/link/module/metadata/system-version/sign path exercised.
- [x] Staged PS5 display/pad/canvas/platform-2D path passed.
- [x] Asset failure narrowed beyond generic module/file-read hypotheses.
- [ ] `TrueTypeFont.Load` hardware boundary resolved.
- [ ] Complete Hello2D visual/input plan passed.
- [ ] Complete Hello3D plan passed.
- [ ] Complete InputDemo plan passed.
- [ ] Complete ModelViewer plan passed.
- [ ] Depth, sampled textures, 3D blending, and mixed CPU/GPU composition independently proven.

Unchecked hardware items are not blockers for publishing an explicitly experimental **alpha source release**; they remain blockers for stronger feature claims.
