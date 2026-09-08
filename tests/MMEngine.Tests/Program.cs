using System;
using System.Numerics;
using System.Text;
using MMEngine.Animation;
using MMEngine.Assets;
using MMEngine.Core;
using MMEngine.Input;
using MMEngine.Scene;

namespace MMEngine.Tests;

internal static class Program
{
    private static int _passed;

    private static int Main()
    {
        try
        {
            Run("transform hierarchy", TransformHierarchy);
            Run("transform reparent rollback", TransformReparentRollback);
            Run("component lifecycle", ComponentLifecycle);
            Run("interface component query", InterfaceComponentQuery);
            Run("clock validation", ClockValidation);
            Run("mesh deterministic round-trip", MeshRoundTrip);
            Run("input edges", InputEdges);
            Run("scene JSON", SceneJson);
            Run("tween completion", TweenCompletion);
            Console.WriteLine($"PASS: {_passed} tests");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("FAIL: " + exception.Message);
            return 1;
        }
    }

    private static void TransformHierarchy()
    {
        var parent = new GameObject("parent");
        var child = new GameObject("child");
        parent.Transform.LocalPosition = new Vector3(1, 0, 0);
        child.Transform.LocalPosition = new Vector3(2, 0, 0);
        child.Transform.SetParent(parent.Transform);
        Equal(3f, child.Transform.Position.X);
        parent.Transform.LocalPosition = new Vector3(5, 0, 0);
        Equal(7f, child.Transform.Position.X);
        Throws<InvalidOperationException>(() => parent.Transform.SetParent(child.Transform));
    }

    private static void ComponentLifecycle()
    {
        using var runtime = new EngineRuntime();
        var scene = new MMEngine.Scene.Scene("test");
        Counter component = scene.CreateGameObject("counter").AddComponent<Counter>();
        runtime.Scenes.SetActive(scene);
        runtime.Tick(0.016);
        runtime.Tick(0.016);
        Equal(1, component.AwakeCount);
        Equal(1, component.StartCount);
        Equal(2, component.UpdateCount);
    }

    private static void TransformReparentRollback()
    {
        var child = new GameObject("child");
        var singularParent = new GameObject("singular");
        singularParent.Transform.LocalScale = Vector3.Zero;
        Throws<InvalidOperationException>(() => child.Transform.SetParent(singularParent.Transform, worldPositionStays: true));
        True(child.Transform.Parent is null, "Failed reparent changed the parent relation.");
    }

    private static void InterfaceComponentQuery()
    {
        using var scene = new MMEngine.Scene.Scene("query");
        scene.CreateGameObject("counter").AddComponent<Counter>();
        int count = 0;
        foreach (ICounterMarker _ in scene.GetComponents<ICounterMarker>())
            count++;
        Equal(1, count);
    }

    private static void ClockValidation()
    {
        var clock = new EngineClock { MaximumDeltaSeconds = 0.1 };
        Near(0.1f, clock.Advance(1).DeltaTime);
        Throws<ArgumentOutOfRangeException>(() => clock.MaximumDeltaSeconds = 0);
        Throws<ArgumentOutOfRangeException>(() => clock.Advance(double.NaN));
    }

    private static void MeshRoundTrip()
    {
        MmMeshAsset source = MeshPrimitives.Cube(0xFF80C0FF);
        byte[] first = MmMeshCodec.Encode(source);
        MmMeshAsset decoded = MmMeshCodec.Decode(first);
        byte[] second = MmMeshCodec.Encode(decoded);
        True(first.AsSpan().SequenceEqual(second), "Mesh encoding is not deterministic.");
        Equal(24, decoded.Vertices.Length);
        Equal(36, decoded.Indices.Length);
        byte[] damaged = (byte[])first.Clone();
        damaged[0] = 0;
        Throws<InvalidOperationException>(() => MmMeshCodec.Decode(damaged));
        byte[] trailing = new byte[first.Length + 1];
        first.CopyTo(trailing, 0);
        Throws<InvalidOperationException>(() => MmMeshCodec.Decode(trailing));
    }

    private static void InputEdges()
    {
        var input = new DualSenseTracker();
        input.Update(DualSenseSnapshot.Neutral with { Connected = true, Buttons = DualSenseButtons.Cross });
        True(input.Pressed(DualSenseButtons.Cross), "Cross should have a pressed edge.");
        input.Update(input.Current);
        True(input.Held(DualSenseButtons.Cross) && !input.Pressed(DualSenseButtons.Cross), "Cross should be held only.");
        input.Update(input.Current with { Buttons = DualSenseButtons.None });
        True(input.Released(DualSenseButtons.Cross), "Cross should have a released edge.");
    }

    private static void SceneJson()
    {
        const string json = "{\"version\":1,\"name\":\"fixture\",\"objects\":[{\"id\":\"root\",\"transform\":{\"position\":[1,2,3]}},{\"id\":\"child\",\"parent\":\"root\",\"components\":[{\"type\":\"counter\"}]}]}";
        using MMEngine.Scene.Scene scene = MmSceneLoader.Load(Encoding.UTF8.GetBytes(json), new Factory());
        Equal(2, scene.GameObjects.Count);
        Equal("root", scene.GameObjects[1].Transform.Parent!.GameObject.Name);
    }

    private static void TweenCompletion()
    {
        float value = 0;
        var tween = new Tween(0, 10, 1, x => value = x, Ease.Linear);
        tween.Update(0.4);
        Near(4f, value);
        tween.Update(0.6);
        Near(10f, value);
        True(tween.IsComplete, "Tween did not complete.");
    }

    private static void Run(string name, Action test)
    {
        test();
        _passed++;
        Console.WriteLine("ok - " + name);
    }

    private static void True(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }

    private static void Equal<T>(T expected, T actual) where T : IEquatable<T>
    {
        if (!expected.Equals(actual))
            throw new InvalidOperationException($"Expected {expected}, got {actual}.");
    }

    private static void Near(float expected, float actual, float tolerance = 0.0001f)
    {
        if (MathF.Abs(expected - actual) > tolerance)
            throw new InvalidOperationException($"Expected {expected} +/- {tolerance}, got {actual}.");
    }

    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }

    private interface ICounterMarker { }

    private sealed class Counter : MMBehaviour, ICounterMarker
    {
        public int AwakeCount { get; private set; }
        public int StartCount { get; private set; }
        public int UpdateCount { get; private set; }
        protected override void Awake() => AwakeCount++;
        protected override void Start() => StartCount++;
        protected override void Update(in FrameTime time) => UpdateCount++;
    }

    private sealed class Factory : ISceneComponentFactory
    {
        public Component Create(string type, System.Text.Json.JsonElement properties) => new Counter();
    }
}
