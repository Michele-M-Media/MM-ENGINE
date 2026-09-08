# Architecture

MM ENGINE keeps portable gameplay state independent from the PS5 backend. Dependencies point downward; the core and content formats can be built and tested on a normal .NET 10 host.

Its lineage is MM API Mapper -> MM PS5 CONTROL -> MM ENGINE, while ownership stays separate. Mapper-style evidence and capability classification shape `EngineDiagnostics`, `Graphics3DCapabilities`, the source audit, and failure logs; MM PS5 CONTROL code is not embedded into the engine, and unavailable health/permission probes are not invented.

| Layer | Projects | Responsibility |
|---|---|---|
| Application | `samples/*` | composition root, resources, scene content, package metadata |
| Platform | `MMEngine.Platform.PS5` | process/display loops, system modules, package I/O, pad conversion, logs |
| Rendering | `Graphics2D`, `Graphics3D`, `UI` | native Surface drawing or AGC command recording |
| Runtime | `Scene`, `Animation`, `Input`, `Assets` | object model, lifecycle, transforms, stable state and formats |
| Foundation | `Core` | clock, time, diagnostics, services, logging |

## Frame ownership

There are deliberately two PS5 application bases:

- `MmApplication2D` derives from SharpProspero `ProsperoApp`. It draws into the CPU Surface and lets the SDK copy/flip the frame. This is the verified path for UI, PNG/TGA, alpha, and text.
- `MmApplication3D` owns the loop and gives `MultiMeshRenderer3D` exactly one AGC submit/flip per frame. This avoids the double-present bug that would result from nesting GPU presentation inside `ProsperoApp.OnFrame`.

The supplied SDK's default tiled scan-out uses a separate linear staging surface for CPU drawing. There is no public, verified staging-to-tiled flush that can be inserted into an AGC frame. For that reason the alpha does not advertise a mixed CPU HUD plus GPU scene path.

## Scene and lifecycle

`EngineRuntime.Tick` advances a monotonic, clamped clock, resets diagnostics, and updates the active scene. `Canvas2D` records logical 2D operations; the AGC renderer records submitted draw calls and triangle counts into the same per-frame diagnostics object. Each active component receives:

1. `Awake` once;
2. `Start` once;
3. `Update` each active frame;
4. `LateUpdate` after regular updates;
5. `OnDestroy` when removed or when its scene is disposed.

Every `GameObject` owns one `Transform`. Parent cycles are rejected. Local matrices use the `System.Numerics` row-vector convention: scale, then rotation, then translation; world is `local * parentWorld`. Scene files use stable string IDs and a caller-supplied component factory, keeping NativeAOT loading reflection-free.

## Rendering boundaries

The 2D layer delegates to verified SharpProspero `Surface` operations, including source-over alpha, scaling, smooth scaling, and nine-slice composition. `Texture2D` owns decoded native memory; `Font2D` owns a TrueType font or exposes the allocation-free bitmap fallback. Consumers must dispose decoded images and outline fonts.

The 3D layer uses the SDK's embedded `mesh_vs.sb` and `mesh_ps.sb`. One command buffer contains the target/linkage state followed by multiple draw packets. Per-draw constant buffers are preallocated per frame in flight. A `.mmmesh` submesh becomes its own `MeshBuffer`, allowing safe public-API submission without patching or vendoring SharpProspero.

Material base color is multiplied into vertex color during upload. UVs and texture names survive conversion, but the built-in pixel shader does not sample textures. The limitation is inspectable at runtime through `Graphics3DCapabilities`.

## Ownership rules

- An application owns `EngineRuntime`, loaded `Texture2D`, `Font2D`, and `GpuMesh3D` resources.
- A `Scene` owns its GameObjects and components, but not arbitrary resources referenced by a renderer component.
- `MultiMeshRenderer3D` owns shaders, command buffers, state blocks, and per-frame constants.
- `Ps5Modules` owns only modules it explicitly loads and unloads them in reverse order.
