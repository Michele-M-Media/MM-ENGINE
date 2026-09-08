# Validation update — 2026-09-08

This document records validation performed after the original static bootstrap was produced. It supplements the original audit/validation documents and should be read together with `STATUS_MATRIX.md`.

## What changed from the original bootstrap statement

The original v0.1-alpha tree correctly stated that its construction environment could not execute `.NET`, the PS5 toolchain, or console hardware. After handoff, the maintainer performed build and staged PS5 validation outside that construction environment.

Accordingly, claims that said "execution pending" are no longer globally accurate. The current status is more granular.

## Build / toolchain validation

Maintainer testing exercised the normal application build path through the stages used by the project:

1. managed/NativeAOT compilation;
2. runtime support collection;
3. PS5 link;
4. module dependency processing;
5. metadata processing;
6. system-version processing;
7. sign/wrap;
8. module-folder output.

The public repository still treats packaging as a distinct gate because package creation depends on external toolchain/environment state and should not be inferred from a successful signed module folder.

## Staged PS5 boot probes

The staged normal-path probes established a useful progression:

| Probe | Purpose | Result |
|---|---|---|
| 0 | SharpProspero display/frame path | hardware PASS |
| 1 | pad/input path | hardware PASS |
| 2 | MM ENGINE canvas integration | hardware PASS |
| 3 | MM ENGINE Platform 2D path | hardware PASS |

Two earlier minimal probes that removed too much of the normal application lifecycle produced `CE-108255-1`; because Probe 0 then passed on the ordinary application path, those artificial failures are not treated as evidence that NativeAOT or the normal bootstrap path is globally broken.

## Asset-path isolation

The original useful crash boundary was between Probe 3 and the aggregate asset-loading Probe 4.

The asset block included:

- `SystemModule.Load(PngDec)`;
- `SystemModule.Load(Font)`;
- `SystemModule.Load(FontFt)`;
- `/app0/assets` reads;
- `TrueTypeFont.Load`;
- PNG decode.

Follow-up staged probes isolated those pieces further. Module loading and direct asset reads advanced successfully. The remaining useful failure boundary was narrowed to the TrueType font construction path, with `TrueTypeFont.Load` the current focal point.

This is materially narrower than the original generic `CE-108255-1` report.

## What is hardware-backed now

At minimum, staged testing demonstrated the ordinary path through:

- NativeAOT application bootstrap on the normal app route;
- `Program.Main`;
- the normal `ProsperoApp` lifecycle;
- system/user services as exercised by the passing app route;
- VideoOut/display initialization;
- framebuffer access;
- present/flip;
- SharpProspero Graphics2D path used by the probes;
- DualSense/input path used by the passing probe;
- `MMEngine.Core`;
- `MMEngine.Scene`;
- `MMEngine.Graphics2D`;
- `MMEngine.Platform.PS5`;
- a real frame loop.

## What remains unproven or blocked

The following must not be described as fully hardware-verified in this alpha:

- complete Hello2D asset/font/PNG UI path;
- complete Hello3D visual correctness;
- correct depth testing;
- sampled 3D material textures;
- 3D alpha blending;
- mixed CPU 2D + tiled AGC GPU composition;
- every InputDemo output behavior;
- full ModelViewer runtime behavior;
- package creation in every target environment.

The engine intentionally does not invent missing AGC state to turn these into claims.

## Diagnostic conclusion

For the current Hello2D-style crash family, investigation should begin at the staged TrueType font-load boundary rather than at generic loader, package, display, or input hypotheses that were already crossed by passing probes.

See `HARDWARE_TEST_LOG.md` and `TROUBLESHOOTING.md` for the operational sequence.
