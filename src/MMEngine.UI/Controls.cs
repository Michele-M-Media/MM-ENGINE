using System;
using MMEngine.Graphics2D;

namespace MMEngine.UI;

public sealed class Label : UiElement
{
    public Font2D? Font { get; set; }
    public string Text { get; set; } = string.Empty;
    public uint ColorArgb { get; set; } = 0xFFFFFFFF;
    public override void Draw(Canvas2D canvas)
    {
        if (Font is not null)
            canvas.DrawText(Font, Text, Bounds.X, Bounds.Y, ColorArgb);
    }
}

public sealed class Image : UiElement
{
    public Texture2D? Texture { get; set; }
    public bool AlphaBlend { get; set; } = true;
    public TextureFilter2D Filter { get; set; } = TextureFilter2D.Nearest;
    public override void Draw(Canvas2D canvas)
    {
        if (Texture is not null)
            canvas.DrawTexture(Texture, Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height, AlphaBlend, Filter);
    }
}

/// <summary>Asset-driven panel whose corners keep their source size as the center stretches.</summary>
public sealed class NineSlicePanel : UiElement
{
    private readonly System.Collections.Generic.List<UiElement> _children = [];

    public Texture2D? Texture { get; set; }
    public int Border { get; set; } = 16;
    public System.Collections.Generic.IReadOnlyList<UiElement> Children => _children;

    public T Add<T>(T child) where T : UiElement
    {
        ArgumentNullException.ThrowIfNull(child);
        _children.Add(child);
        return child;
    }

    public override void Draw(Canvas2D canvas)
    {
        if (Texture is not null)
            canvas.DrawNineSlice(Texture, Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height, Math.Max(0, Border));
        for (int i = 0; i < _children.Count; i++)
            if (_children[i].Visible)
                _children[i].Draw(canvas);
    }
}

public sealed class Button : UiElement
{
    public Font2D? Font { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool Selected { get; set; }
    public bool Pressed { get; set; }
    public uint NormalArgb { get; set; } = 0xFF253149;
    public uint SelectedArgb { get; set; } = 0xFF3974E8;
    public uint PressedArgb { get; set; } = 0xFF1F55B9;

    public override void Draw(Canvas2D canvas)
    {
        uint color = Pressed ? PressedArgb : Selected ? SelectedArgb : NormalArgb;
        canvas.FillRoundedRect(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height, 10, color);
        if (Font is null)
            return;
        int x = Bounds.X + Math.Max(12, (Bounds.Width - Font.Measure(Text)) / 2);
        int y = Bounds.Y + Math.Max(0, (Bounds.Height - Font.LineHeight) / 2);
        canvas.DrawText(Font, Text, x, y, 0xFFFFFFFF);
    }
}

public sealed class ProgressBar : UiElement
{
    private float _value;
    public float Value { get => _value; set => _value = Math.Clamp(value, 0f, 1f); }
    public uint TrackArgb { get; set; } = 0xFF192131;
    public uint FillArgb { get; set; } = 0xFF38C6F4;
    public override void Draw(Canvas2D canvas)
    {
        canvas.FillRoundedRect(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height, Bounds.Height / 2, TrackArgb);
        int fill = (int)MathF.Round(Bounds.Width * Value);
        if (fill > 0)
            canvas.FillRoundedRect(Bounds.X, Bounds.Y, fill, Bounds.Height, Bounds.Height / 2, FillArgb);
    }
}

public sealed class Slider : UiElement
{
    private float _value;
    public float Value { get => _value; set => _value = Math.Clamp(value, 0f, 1f); }
    public uint TrackArgb { get; set; } = 0xFF28354C;
    public uint AccentArgb { get; set; } = 0xFF58D0F4;
    public override void Draw(Canvas2D canvas)
    {
        int cy = Bounds.Y + Bounds.Height / 2;
        canvas.FillRoundedRect(Bounds.X, cy - 3, Bounds.Width, 6, 3, TrackArgb);
        int position = Bounds.X + (int)MathF.Round((Bounds.Width - 1) * Value);
        canvas.FillRoundedRect(position - 8, cy - 8, 16, 16, 8, Enabled ? AccentArgb : 0xFF667080);
    }
}

public sealed class Card : UiElement
{
    public Font2D? Font { get; set; }
    public Texture2D? Artwork { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public bool Selected { get; set; }

    public override void Draw(Canvas2D canvas)
    {
        canvas.FillRoundedRect(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height, 18,
            Selected ? 0xFF344B70 : 0xE8263042);
        if (Artwork is not null)
            canvas.DrawTexture(Artwork, Bounds.X + 12, Bounds.Y + 12, Bounds.Width - 24,
                Math.Max(1, Bounds.Height - 88), true, TextureFilter2D.Nearest);
        if (Font is null)
            return;
        canvas.DrawText(Font, Title, Bounds.X + 16, Bounds.Y + Bounds.Height - 65, 0xFFFFFFFF);
        canvas.DrawText(Font, Subtitle, Bounds.X + 16, Bounds.Y + Bounds.Height - 34, 0xFF9FB1C8);
    }
}
