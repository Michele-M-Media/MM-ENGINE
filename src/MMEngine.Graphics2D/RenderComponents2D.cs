using System;
using System.Collections.Generic;
using System.Numerics;
using MMEngine.Scene;

namespace MMEngine.Graphics2D;

public interface IDrawable2D
{
    int Order { get; }
    void Draw(Canvas2D canvas, Vector2 cameraPosition);
}

public sealed class SpriteRenderer2D : Component, IDrawable2D
{
    public Texture2D? Texture { get; set; }
    public Vector2 Size { get; set; }
    public Vector2 Pivot { get; set; } = new(0.5f, 0.5f);
    public bool AlphaBlend { get; set; } = true;
    public TextureFilter2D Filter { get; set; }
    public int Order { get; set; }

    public void Draw(Canvas2D canvas, Vector2 cameraPosition)
    {
        if (!Enabled || Texture is null)
            return;
        Vector3 world = Transform.Position;
        Vector3 scale = new(Transform.WorldMatrix.M11, Transform.WorldMatrix.M22, Transform.WorldMatrix.M33);
        int width = (int)MathF.Round((Size.X <= 0 ? Texture.Width : Size.X) * MathF.Abs(scale.X));
        int height = (int)MathF.Round((Size.Y <= 0 ? Texture.Height : Size.Y) * MathF.Abs(scale.Y));
        int x = (int)MathF.Round(world.X - cameraPosition.X - width * Pivot.X);
        int y = (int)MathF.Round(world.Y - cameraPosition.Y - height * Pivot.Y);
        canvas.DrawTexture(Texture, x, y, width, height, AlphaBlend, Filter);
    }
}

public sealed class TextRenderer2D : Component, IDrawable2D
{
    public Font2D? Font { get; set; }
    public string Text { get; set; } = string.Empty;
    public uint ColorArgb { get; set; } = 0xFFFFFFFF;
    public int Order { get; set; }

    public void Draw(Canvas2D canvas, Vector2 cameraPosition)
    {
        if (!Enabled || Font is null || Text.Length == 0)
            return;
        Vector3 world = Transform.Position;
        canvas.DrawText(Font, Text,
            (int)MathF.Round(world.X - cameraPosition.X),
            (int)MathF.Round(world.Y - cameraPosition.Y), ColorArgb);
    }
}

public sealed class Camera2DComponent : Component
{
    public Vector2 Position => new(Transform.Position.X, Transform.Position.Y);
}

public sealed class Scene2DRenderer
{
    private readonly List<IDrawable2D> _drawables = [];

    public void Render(MMEngine.Scene.Scene scene, Canvas2D canvas, Camera2DComponent? camera = null)
    {
        ArgumentNullException.ThrowIfNull(scene);
        _drawables.Clear();
        foreach (IDrawable2D drawable in scene.GetComponents<IDrawable2D>())
            _drawables.Add(drawable);
        _drawables.Sort(static (a, b) => a.Order.CompareTo(b.Order));
        Vector2 cameraPosition = camera?.Position ?? Vector2.Zero;
        for (int i = 0; i < _drawables.Count; i++)
            _drawables[i].Draw(canvas, cameraPosition);
    }
}
