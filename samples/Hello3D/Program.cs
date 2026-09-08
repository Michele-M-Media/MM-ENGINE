using System.Numerics;
using MMEngine.Animation;
using MMEngine.Assets;
using MMEngine.Core;
using MMEngine.Graphics3D;
using MMEngine.Input;
using MMEngine.Platform.PS5;
using MMEngine.Scene;
using SharpProspero.Application;

namespace MMEngine.Samples.Hello3D;

internal sealed class Game : MmApplication3D
{
    private GpuMesh3D? _cube;
    private GpuMesh3D? _background;
    private Camera3DComponent? _camera;
    private Scene3DRenderer? _sceneRenderer;

    protected override void OnEngineLoad()
    {
        _cube = GpuMesh3D.Upload(MeshPrimitives.Cube(0xFFFFFFFF));
        _background = GpuMesh3D.Upload(MeshPrimitives.Quad(0xFF07101F));
        var scene = new MMEngine.Scene.Scene("Hello3D");

        GameObject backdrop = scene.CreateGameObject("Background");
        backdrop.Transform.SetLocal(new Vector3(0, 0, -3), Quaternion.Identity, new Vector3(18, 10, 1));
        backdrop.AddComponent(new MeshRenderer3D { Mesh = _background, Order = -100 });

        for (int i = 0; i < 3; i++)
        {
            GameObject cube = scene.CreateGameObject("Cube" + i.ToString());
            cube.Transform.LocalPosition = new Vector3((i - 1) * 1.35f, 0, 0);
            cube.AddComponent(new MeshRenderer3D { Mesh = _cube, Order = i });
            cube.AddComponent(new TransformSpinner { RadiansPerSecond = 0.55f + i * 0.35f });
        }
        GameObject cameraObject = scene.CreateGameObject("Camera");
        cameraObject.Transform.LocalPosition = new Vector3(0, 0, 5);
        _camera = cameraObject.AddComponent<Camera3DComponent>();
        Engine.Scenes.SetActive(scene);
        _sceneRenderer = new Scene3DRenderer(Renderer);
    }

    protected override void OnEngineFrame(MultiMeshRenderer3D renderer, in FrameTime time)
    {
        if (Input.Held(DualSenseButtons.Options | DualSenseButtons.TouchPad))
            RequestExit();
        _sceneRenderer!.Render(Engine.Scenes.ActiveScene!, _camera!, Config.Width, Config.Height);
    }

    protected override void OnEngineUnload()
    {
        _cube?.Dispose();
        _background?.Dispose();
    }
}

internal static class Program
{
    private static void Main()
    {
        using (var game = new Game())
            game.Run();
        ProcessExit.Exit();
    }
}
