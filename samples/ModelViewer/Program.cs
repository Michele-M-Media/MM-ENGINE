using System;
using System.Numerics;
using MMEngine.Assets;
using MMEngine.Core;
using MMEngine.Graphics3D;
using MMEngine.Input;
using MMEngine.Platform.PS5;
using SharpProspero.Application;

namespace MMEngine.Samples.ModelViewer;

internal sealed class Game : MmApplication3D
{
    private GpuMesh3D? _model;
    private GpuMesh3D? _background;
    private float _yaw;
    private float _pitch;
    private float _distance = 4f;

    protected override void OnEngineLoad()
    {
        MmMeshAsset asset = Ps5Assets.LoadMesh("/app0/assets/model.mmmesh");
        _model = GpuMesh3D.Upload(asset);
        _background = GpuMesh3D.Upload(MeshPrimitives.Quad(0xFF07101C));
    }

    protected override void OnEngineFrame(MultiMeshRenderer3D renderer, in FrameTime time)
    {
        if (Input.Held(DualSenseButtons.Options | DualSenseButtons.TouchPad))
            RequestExit();
        _yaw += Input.Current.RightStick.X * time.DeltaTime * 2.5f;
        _pitch += Input.Current.RightStick.Y * time.DeltaTime * 2.0f;
        _distance = Math.Clamp(_distance - (Input.Current.RightTrigger - Input.Current.LeftTrigger) * time.DeltaTime * 2f, 2f, 9f);

        Matrix4x4 view = Matrix4x4.CreateLookAt(new Vector3(0, 0, _distance), Vector3.Zero, Vector3.UnitY);
        Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(1.0f, (float)Config.Width / Config.Height, 0.05f, 100f);
        Matrix4x4 viewProjection = view * projection;
        Matrix4x4 model = Matrix4x4.CreateRotationX(_pitch) * Matrix4x4.CreateRotationY(_yaw);
        Matrix4x4 background = Matrix4x4.CreateScale(18, 10, 1) * Matrix4x4.CreateTranslation(0, 0, -4);

        renderer.BeginFrame();
        renderer.Draw(_background!, background, viewProjection);
        renderer.Draw(_model!, model, viewProjection);
        renderer.EndFrame();
    }

    protected override void OnEngineUnload()
    {
        _model?.Dispose();
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
