# API overview

MM ENGINE separates portable gameplay/content APIs from the SharpProspero-backed PS5 layer. The routine path never requires an application to record AGC packets directly.

| Namespace | Main public surface | Purpose |
|---|---|---|
| `MMEngine.Core` | `EngineClock`, `FrameTime`, `Time`, `EngineDiagnostics`, `EngineLog`, `ServiceRegistry` | frame state, diagnostics, logging, explicit services |
| `MMEngine.Scene` | `EngineRuntime`, `SceneManager`, `Scene`, `GameObject`, `Component`, `MMBehaviour`, `Transform`, `MmSceneLoader` | lifecycle, hierarchy, reflection-free text scenes |
| `MMEngine.Assets` | `AssetId`, `AssetRegistry`, `MmMeshAsset`, `MmMeshCodec`, `MmMaterial`, `MmSubMesh`, `MeshPrimitives` | deterministic content and runtime asset lookup |
| `MMEngine.Input` | `DualSenseSnapshot`, `DualSenseTracker`, `DualSenseInput`, `IDualSenseSource` | stable per-frame state, edges, output controls |
| `MMEngine.Animation` | `Tween`, `Ease`, `TransformSpinner` | portable value and transform animation |
| `MMEngine.Graphics2D` | `Canvas2D`, `Texture2D`, `Font2D`, `SpriteRenderer2D`, `TextRenderer2D`, `Scene2DRenderer` | native Surface images, text, shapes, alpha and scale |
| `MMEngine.UI` | `UiCanvas`, `Panel`, `NineSlicePanel`, `Card`, `Label`, `Image`, `Button`, `Slider`, `ProgressBar` | code-authored native UI |
| `MMEngine.Graphics3D` | `GpuMesh3D`, `MultiMeshRenderer3D`, `Camera3DComponent`, `MeshRenderer3D`, `Scene3DRenderer`, `Graphics3DCapabilities` | mesh upload and multi-draw AGC frames |
| `MMEngine.Platform.PS5` | `MmApplication2D`, `MmApplication3D`, `Ps5Assets`, `Ps5Input`, `Ps5DualSenseSource`, `Ps5Modules` | console lifecycle, package files, modules and pad adapter |

## Portable behavior

```csharp
public sealed class Spin : MMBehaviour
{
    public float Speed { get; set; } = 1f;

    protected override void Update(in FrameTime time)
        => Transform.RotateY(Speed * time.DeltaTime);
}

using var runtime = new EngineRuntime();
var scene = new Scene("Demo");
scene.CreateGameObject("Hero").AddComponent<Spin>();
runtime.Scenes.SetActive(scene);
runtime.Tick(1.0 / 60.0);
```

`Awake` and `Start` run once before the first enabled update. `LateUpdate` follows regular updates, and `OnDestroy` runs when a component is removed or its scene is disposed.

## Native frame entry points

Derive a 2D application from `MmApplication2D`, opt into PNG and/or TrueType modules, load resources in `OnEngineLoad`, and draw through `Canvas2D` in `OnEngineFrame`. The base owns the CPU framebuffer present loop.

Derive a 3D application from `MmApplication3D`, upload `MmMeshAsset` instances through `GpuMesh3D.Upload`, then issue exactly one `BeginFrame` / zero-or-more `Draw` / `EndFrame` sequence. `Scene3DRenderer` performs that sequence for scene components. The base owns the AGC flip loop.

Do not combine the two frame owners in this alpha. Inspect `Graphics3DCapabilities` before selecting a material feature: sampled textures and depth are deliberately false until the verified SDK gates in `ROADMAP.md` are met.

## Resource ownership

Applications dispose `Texture2D`, `Font2D`, and `GpuMesh3D`. A `Scene` owns its objects/components but not renderer resources referenced by those components. The application bases own their engine/platform state; `MultiMeshRenderer3D` owns its shader and per-frame AGC allocations.

The complete working examples are under `samples`; CLI and content-format commands are documented in [BUILD_AND_CLI.md](BUILD_AND_CLI.md) and [ASSET_PIPELINE.md](ASSET_PIPELINE.md).
