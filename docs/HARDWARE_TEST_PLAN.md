# PS5 hardware test plan

Hardware execution is the final authority. Run this plan on a console/environment where you are authorized to install and test homebrew packages.

## Before the run

1. Record the SharpProspero archive hash and repository commit/archive hash.
2. Run `scripts/doctor.ps1` and save its output.
3. Run the portable suite and `scripts/build-all-samples.ps1 -Output Package`.
4. Confirm each build has `out/module/eboot.bin`, one package, and a complete `artifacts/logs` transcript.
5. Use distinct development title IDs if existing installations would collide.
6. Attach a development-console log and start continuous video capture before launch.

## Test A: Hello2D

Expected:

- launch reaches the MM ENGINE panel without a crash or system error dialog;
- dark background and cyan rail fill the whole intended regions without stride corruption;
- the PNG mark has clean transparent corners and is alpha-scaled inside the selected card;
- TrueType text edges are antialiased, correctly colored, and stable across frames;
- holding Cross changes the button state; R2 fills the progress bar continuously;
- holding Options + Touch Pad exits cleanly without the platform reporting an unexpected application close.

Repeat for at least 300 frames. Watch for flicker, wrong red/blue channel order, alpha halos, stale buffers, glyph corruption, and steadily increasing memory.

## Test B: InputDemo

Expected:

- connection state changes correctly after controller sleep/reconnect;
- each face button lights only its own tile;
- a quick tap displays a pressed frame and then a released frame; holding does not retrigger pressed;
- both sticks reach all quadrants, settle near center, and do not swap axes;
- both triggers sweep from zero to full independently;
- pressing Cross starts both vibration motors and changes the light bar; release stops vibration;
- holding Options + Touch Pad exits.

For a targeted instrumentation build, log two touch contacts, orientation, acceleration, angular velocity, and monotonic sample timestamps. These values are exposed by the stable API even though the visual sample focuses on controls needed by the other demos.

## Test C: Hello3D

Expected:

- three cubes appear in one frame and rotate independently;
- the full-screen geometry backdrop is behind them and has stable color;
- geometry is not mirrored, index-corrupted, or red/blue swapped;
- no double flip, alternating stale buffer, or periodic blank frame occurs;
- holding Options + Touch Pad exits.

Move the draw order in a diagnostic build and confirm that overlap changes, demonstrating the documented painter-order fallback. Do not record this as depth success: the alpha has no depth buffer.

## Test D: ModelViewer

Expected:

- the fixture loads from `/app0/assets/model.mmmesh` and all three named submeshes render;
- dark pedestal, cyan crystal, and violet band remain visually distinct;
- right stick rotates yaw/pitch; L2/R2 zoom within the fixed range;
- rapid movement does not exceed command-buffer reservation or corrupt later frames;
- holding Options + Touch Pad exits.

Then locally import a legally usable multi-mesh GLB with UVs/materials, replace `model.mmmesh`, rebuild, and verify submesh count and base-color fallback. A texture filename surviving conversion is an asset-pipeline pass, not a texture-sampling pass.

## Failure triage

Classify the first failure:

| Symptom | First evidence to collect |
|---|---|
| compile/link | full transcript plus first unresolved symbol and fresh `.o` check |
| package/install | package tool output, metadata, title/content IDs |
| launch fault | console log from process start and exact system dialog/code |
| blank/stale 2D | tiling mode, buffer index, present/stride logs, video |
| blank/stale 3D | AGC init/link/submit return, command dword count, buffer index |
| geometry corruption | mesh validator output, counts, submesh ranges, MVP matrices |
| input missing | initial user ID, pad open result, connected flag, raw timestamp |

Fix one layer at a time and rerun the smallest affected sample before the whole suite.
