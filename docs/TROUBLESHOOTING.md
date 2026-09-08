# Troubleshooting

Use the narrowest failing stage. Do not treat every `CE-108255-1` as the same fault.

## 1. Environment / doctor failure

Run:

```powershell
./scripts/doctor.ps1
```

Check that:

- `.NET 10` is available;
- on Windows, WSL has `.NET 10`;
- `SHARPPROSPERO_ROOT` points to the SDK root, not a nested source directory;
- the pinned SharpProspero baseline files match `eng/sdk-baseline.json`.

A modified/contaminated SDK copy can make a later renderer or build result meaningless.

## 2. Portable build failure

Run:

```powershell
dotnet restore MMEngine.Portable.slnx
dotnet build MMEngine.Portable.slnx -c Release
dotnet run --project tests/MMEngine.Tests/MMEngine.Tests.csproj -c Release
```

Fix portable failures before testing PS5-facing projects.

## 3. NativeAOT publish produces no object

The driver deletes the previous object before publish. Inspect the publish output and ensure a new object appears under:

`obj/Release/net10.0/linux-x64/native/`

Do not accept an old object from a prior build as success.

## 4. Link / Modules / Metadata / System version / Sign

Identify the first named pipeline stage that fails. These stages are sequential; a successful link does not imply a successful signed module folder.

Use `-Output Folder` while debugging so package creation is not mixed into runtime isolation.

## 5. Install/runtime crash

Distribute/test the complete signed `out/module` tree, not only `eboot.bin`, whenever the sample requires `sce_sys`, `sce_module`, or assets.

## 6. `CE-108255-1` before first frame

Use staged probes. For the current v0.1-alpha history, the normal display/pad/canvas/platform-2D path crossed hardware successfully. The useful remaining asset boundary is TrueType font construction.

Therefore, for the known Hello2D-style failure:

1. keep display/frame path unchanged;
2. keep module loads that already passed;
3. keep direct asset read that already passed;
4. isolate `TrueTypeFont.Load`;
5. only after that passes, add glyph drawing;
6. then isolate PNG decode;
7. only then restore the complete UI composition.

## 7. Black screen / flicker / wrong colors

Check:

- framebuffer dimensions and format;
- CPU staging vs scan-out layout;
- channel order;
- present/flip ownership;
- whether CPU and GPU paths are both trying to own the same frame.

Do not guess AGC register values to "fix" a black screen.

## 8. Input problems

First distinguish:

- pad open/connection;
- button/stick snapshot;
- edge tracking;
- vibration/light-bar output.

A display/frame PASS does not automatically validate every output command.

## 9. 3D limitations that are not bugs in v0.1-alpha

The following remain intentionally unclaimed:

- verified depth buffer path;
- sampled 3D textures;
- 3D alpha blending;
- mixed CPU UI + tiled GPU composition.

See `KNOWN_LIMITATIONS.md` and `STATUS_MATRIX.md`.
