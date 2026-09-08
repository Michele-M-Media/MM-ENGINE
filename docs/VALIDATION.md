# Validation record

## Original bootstrap validation

The initial construction pass:

- inspected the supplied task/reference material and SDK audit;
- compared normalized SDK source trees;
- checked repository JSON/XML structure;
- statically checked C# source structure and references;
- validated the deterministic `.mmmesh` fixture structure;
- verified package asset presence and title/content ID shape;
- excluded development-only controller/reference material from the clean source archive.

That construction environment itself did not contain the full executable toolchain or PS5 hardware, so the original documents correctly kept runtime claims pending.

## Subsequent maintainer validation

After handoff, external maintainer testing exercised:

- portable build/tests;
- the normal NativeAOT application build path through link/module/metadata/system-version/sign stages;
- staged PS5 boot probes.

The passing staged normal-path probes reached:

- display/frame/present;
- pad/input;
- MM ENGINE canvas;
- MM ENGINE Platform 2D.

Follow-up asset isolation advanced through module loading and direct asset reads and narrowed the remaining useful failure boundary to the TrueType font construction path (`TrueTypeFont.Load`).

See:

- `VALIDATION_UPDATE_2026-09-08.md`;
- `HARDWARE_STATUS.md`;
- `HARDWARE_TEST_LOG.md`.

## Validation that still remains

Do not infer success for:

- complete Hello2D asset/font/PNG/UI execution;
- complete Hello3D visual correctness;
- correct depth;
- sampled 3D textures;
- 3D material alpha blending;
- mixed CPU/GPU tiled composition;
- every InputDemo output behavior;
- full ModelViewer runtime;
- every packaging environment.

The exact status matrix is authoritative for the public alpha.
