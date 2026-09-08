# Bootstrap source audit

This audit records what was examined before implementation. Paths below are relative to the supplied work package or a SharpProspero 0.8 SDK root.

## Input integrity

All eight `00_READ_FIRST` documents were read before source design. The package checksum list validated every referenced content file except its own `SHA256SUMS.txt` entry. A checksum file cannot retain a valid checksum of itself after that checksum is embedded; this is recorded as a manifest-construction defect rather than silently treated as content corruption.

Archive hashes used during the audit:

| Input | SHA-256 |
|---|---|
| `SharpProspero-0.8.zip` | `db85157b7d46ba6d19b9bb995395fcdd2f7d28b2cf619aa5e1f4a0d573334054` |
| `SharpProspero-SDK-v0.8.zip` | `b36ed7c93e3b4b7296eef3bd999886e2d3b1b62294585e8700bba9429e82b8c1` |
| reference v0.12 | `1d3e09133301164fee0f16fee52a4fcb9efe87cc6a412c1d79bb7db0da4e4432` |
| reference v0.13 | `4440ddc0db2d90648e5b98c16ad1ddd84879ec31a57089f023a1af88f79159a5` |
| supplied GLB | `cb515915d13a50714f19088166faa3f454b9ba4deeaa36eb719c20ccb71d82c0` |
| supplied OBJ | `13117cf4336739eb5e81a1c177a1102fb6e6f8a125f9e33c7d3ff9fddf0afa8a` |

The two SDK archives have different ZIP hashes and top-level folder names, but recursive comparison of the normalized extracted roots found no content differences: 585 files and approximately 7.1 MB in each tree. MM ENGINE therefore targets one SharpProspero 0.8 baseline rather than maintaining false A/B variants.

## Verified SDK surfaces

| Area | Source inspected | Finding used by MM ENGINE |
|---|---|---|
| 2D | `Graphics/Surface*.cs` | clipped primitives, alpha blit, scaled blit, smooth scale, nine-slice |
| PNG/TGA | `Graphics/PngImage.cs`, `TgaImage.cs` | native decoders; PNG needs `SystemModuleId.PngDec`, TGA needs none |
| text | `Graphics/TrueTypeFont.cs`, `ITextFont.cs`, `TextLayout.cs` | coverage-based AA glyphs; Font and FontFt modules; bitmap fallback |
| input | `Input/GamePad.cs`, `Interop/Pad/Pad.cs` | stable pad mask, centered raw axes, analog triggers, motion, touch, timestamp |
| mesh | `Graphics/Vertex.cs`, `MeshData.cs`, `Agc/MeshBuffer.cs` | exact 36-byte position/normal/UV/color layout and 32-bit indices |
| shaders | `Agc/BuiltInShaders.cs`, `ShaderBinary.cs`, embedded PSSL/SB | embedded vertex/pixel binaries; vertex resource reflection; fixed lit color pixel path |
| AGC draw | `Graphics/Renderer3D.cs`, `Agc/DrawCommandBuffer.cs`, `AgcDevice.cs` | target/linkage/state setup, indirect registers, indexed draw, timeline flip |
| display | `Graphics/DisplayDevice.cs` | tiled scan-out plus CPU staging, CPU present copy, GPU-visible target address |
| texture tools | `tools/SharpProspero.Texture/*` | host PNG/TGA/BMP/QOI decoding, linear GNF encoding and header reader; library only |
| build | `build/Prospero.App.props`, `.targets`, `build-app.ps1`, `doctor.ps1` | .NET 10 NativeAOT, WSL compile on Windows, explicit Prospero link/wrap/package path |

## Reference application delta

The v0.13 reference contains a locally changed SharpProspero copy. Compared with the stock SDK it adds an `AutoPresent` switch, a CPU staging flush helper, and `Renderer3D.BeginFrame` / `DrawMeshInFrame` / `EndFrame` with per-draw constants. The engine adopts the multi-draw algorithm inside its own `MultiMeshRenderer3D`; it does not patch the external SDK.

The reference does not add depth buffering or a texture-sampling pixel shader. Its converted controller pieces retain UVs and approximate textured appearance through vertex color. This is why MM ENGINE separates asset retention/preprocessing from renderer capability and keeps those missing GPU paths blocked.

The reference's successful build history also removes general `DateTime` formatting that pulled unavailable globalization/time-zone linkage into NativeAOT. MM ENGINE application code avoids that pattern and leaves logging to the SDK's device-safe implementation.

## Exact blockers

Depth-related types (`CxDepthRenderTarget`, depth/stencil control, surface tiling) exist. What is absent is the verified register-default block/offset sequence for a depth target and a demonstrated clear/bind/order sequence. Synthesizing those register values would violate the mission's no-fake-API rule.

Texture/sampler descriptor types and GNF writing exist. The embedded pixel shader does not declare a sampled image; no shader compiler ships in the package; and only the geometry/vertex user-data base used by the stock renderer is exposed in a verified binding path. Guessing a pixel user-data base or a compiled binary interface is not acceptable.

Blend register wrappers exist, but there is no verified state recipe in the supplied renderer path. 2D source-over alpha remains fully available on the native Surface.

## Supplied evidence

The target UI image and DualSense image were used as visual direction only. The supplied 19.338-second, 1920x1080, 30 fps video confirms the native dashboard direction. Error screenshots show a legacy backend ELF dependency and a wrapper that hid the causal lines behind `Link failed`; the MM ENGINE wrapper retains complete transcripts and never depends on that legacy ELF.
