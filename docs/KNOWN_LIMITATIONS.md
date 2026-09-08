# Known limitations

- No verified 3D depth buffer. Draw order is explicit and intersections can be wrong.
- No 3D sampled textures or material alpha blend. UVs, texture names, GNF preprocessing, and material metadata are preserved for a future verified shader path; rendering uses vertex/base color.
- No supported mixed CPU Surface UI over a tiled AGC frame. Use separate 2D and 3D applications in this alpha.
- The verified 2D API has separate smooth opaque scaling and alpha-blended scaling paths, but no combined smooth-plus-alpha call; alpha images use the blended scaler.
- The GLB importer supports the common uncompressed single-embedded-buffer triangle subset and `KHR_texture_transform`, not sparse accessors, Draco, external buffers, skins, morph targets, animations, non-triangle modes, or advanced material rendering.
- The OBJ normal generator smooths shared position/UV/normal tuples. Export explicit split normals for hard edges.
- Scene component creation is intentionally reflection-free; applications must implement `ISceneComponentFactory`.
- The UI layer provides rendering primitives and state, not a full focus/navigation system or text input.
- Controller-open retry is provided by both the SharpProspero 2D base loop and MM ENGINE's custom GPU loop. A disconnected controller handle relies on the platform to resume samples when the same device reconnects.
- Development title/content IDs in samples are placeholders and may collide outside the test environment.
- The repository does not vendor SharpProspero, PS5 SDK/proprietary material, the task's controller model, or a shader compiler.
- NativeAOT compilation, link, package creation, install, and console behavior have not been executed in the repository-construction environment; complete them before any release claim.
