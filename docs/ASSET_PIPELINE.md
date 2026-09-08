# Asset pipeline

## Native mesh format: `.mmmesh` v1

The format is deterministic, little-endian, bounds-checked, and designed for direct conversion to the SharpProspero 36-byte vertex layout. All section offsets and counts are unsigned 32-bit values.

| Header offset | Field |
|---:|---|
| `0x00` | magic `MMSH` |
| `0x04` | version `1` |
| `0x08` | endian marker `0x01020304` |
| `0x0C` | header size `64` |
| `0x10` | vertex stride `36` |
| `0x14..0x24` | vertex, index, submesh, material, string-byte counts |
| `0x28..0x38` | vertex, index, submesh, material, string-table offsets |
| `0x3C` | flags, zero in v1 |

Each vertex is position `float3`, normal `float3`, UV `float2`, then packed ARGB. Indices are 32-bit triangle-list indices. A submesh record contains first index, index count, material index, and a UTF-8 name offset. A material record retains name, optional base-color texture name, packed base color, flags, metallic, roughness, and a reserved sampler word. Strings are unique, insertion-ordered UTF-8 strings terminated with zero.

The decoder rejects unknown versions, non-canonical or overlapping/gapped sections, trailing bytes, non-zero reserved data, out-of-file ranges, malformed UTF-8, non-contiguous submesh coverage, invalid triangle ranges, missing/inconsistent materials, out-of-range indices, and non-finite geometry.

## OBJ import

The CLI supports:

- `v`, including optional RGB/RGBA vertex color;
- `vt` and optional `--flip-v`;
- `vn`, with normalized generated normals when absent;
- positive and negative indices;
- polygon fan triangulation;
- `o`, `g`, `usemtl`, and one or more `mtllib` declarations;
- MTL `Kd`, `d`, `Tr`, and `map_Kd`.

Missing MTL files and undefined materials are explicit warnings, never silent. Texture names remain metadata and base color provides the current 3D fallback.

## GLB import

The CLI reads glTF 2.0 binary containers with one embedded BIN chunk. It traverses the active scene and node hierarchy, applies node matrix/TRS transforms, transforms normals by inverse transpose, and imports every triangle primitive as a named submesh. It retains `POSITION`, optional `NORMAL`, `TEXCOORD_0`, `COLOR_0`, PBR base-color factor/texture, metallic, roughness, alpha-blend mode, and double-sided flags. `KHR_texture_transform` offset, scale, and rotation are baked into each primitive's UVs.

Current explicit rejections are missing/external/multiple buffers, sparse accessors, Draco compression, non-triangle primitive modes, unsupported attribute encodings, nonzero texture-coordinate sets, unsupported required extensions, skins, morph targets, and animations. Embedded image payloads are named in material metadata but not extracted. Alpha-mask cutoffs, non-base-color maps, emissive data, and optional material extensions produce explicit fallback warnings rather than being silently represented. Export an uncompressed, embedded-buffer GLB with any desired pose baked before import.

## Rebuilding the fixture

```powershell
./scripts/build-assets.ps1
```

This imports `examples/models/model-viewer.obj`, writes `samples/ModelViewer/assets/model.mmmesh`, runs the conversion twice to enforce byte-for-byte determinism, and performs canonical read-back validation. The fixture has three named submeshes and four material entries including the default.

## Textures

Use `mmtexture` to create a validated linear GNF for future/custom shader work. Runtime 2D should normally keep PNG (after loading `PngDec`) or TGA. Runtime 3D in this alpha preserves the texture reference but uses material/vertex color, because the supplied SDK has no verified pixel user-data binding path for the built-in shader and ships no PSSL compiler.

## Supplied DualSense reference

The task's `ps5.controller.glb` was inspected locally: glTF 2.0, 12 meshes, 17 materials, five textures/two images, no animations, triangle primitives, supported attribute encodings, and a required `KHR_texture_transform`. It fits the importer's geometry/UV subset; unsupported advanced material properties are reported as fallbacks. The OBJ references an absent `ps5.controller.mtl`. Neither source nor derived geometry is redistributed here because the task explicitly classifies it as local material. Users may convert a separately licensed model and replace the ModelViewer fixture.
