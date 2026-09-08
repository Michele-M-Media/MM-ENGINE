# Hardware status — v0.1-alpha

This file is the concise hardware-facing status. It intentionally separates **build success**, **hardware success**, **experimental**, and **blocked**.

## Confirmed by staged hardware probes

- normal application path reaches `Program.Main`;
- display initialization and framebuffer path;
- real present/flip loop;
- pad/input path used by the staged probe;
- MM ENGINE core/scene/canvas/platform-2D integration up through Probe 3.

## Asset path

The aggregate asset probe originally crashed before the first completed frame. Subsequent staged isolation advanced through module loading and direct `/app0/assets` reads and narrowed the remaining useful failure boundary to TrueType font construction (`TrueTypeFont.Load`).

Therefore:

- PngDec module load: passed staged isolation;
- Font / FontFt module load: passed staged isolation;
- direct asset read: passed staged isolation;
- TrueType font load: current failure boundary;
- complete Hello2D font+PNG UI sequence: not yet hardware-cleared.

## 3D

`MMEngine.Graphics3D` contains a source-verified experimental multi-draw path based on SharpProspero's built-in shader flow. This alpha does not claim hardware-verified depth, sampled textures, 3D blending, or a fully validated ModelViewer frame.

## Status rule

A source implementation is not promoted to "hardware working" merely because it compiles or because a neighboring probe passes. Hardware status is attached to the exact staged behavior that was exercised.
