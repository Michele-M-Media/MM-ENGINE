using System;
using System.Collections.Generic;
using MMEngine.Graphics2D;

namespace MMEngine.UI;

public readonly record struct RectI(int X, int Y, int Width, int Height)
{
    public bool Contains(int x, int y) => x >= X && y >= Y && x < X + Width && y < Y + Height;
}

public abstract class UiElement
{
    public RectI Bounds { get; set; }
    public bool Visible { get; set; } = true;
    public bool Enabled { get; set; } = true;
    public abstract void Draw(Canvas2D canvas);
}

public sealed class UiCanvas
{
    private readonly List<UiElement> _elements = [];
    public IReadOnlyList<UiElement> Elements => _elements;
    public T Add<T>(T element) where T : UiElement
    {
        ArgumentNullException.ThrowIfNull(element);
        _elements.Add(element);
        return element;
    }

    public void Draw(Canvas2D canvas)
    {
        for (int i = 0; i < _elements.Count; i++)
            if (_elements[i].Visible)
                _elements[i].Draw(canvas);
    }
}

public sealed class Panel : UiElement
{
    private readonly List<UiElement> _children = [];
    public uint BackgroundArgb { get; set; } = 0xD0202634;
    public uint BorderArgb { get; set; } = 0x604F6B8A;
    public int Radius { get; set; } = 16;
    public IReadOnlyList<UiElement> Children => _children;

    public T Add<T>(T child) where T : UiElement
    {
        ArgumentNullException.ThrowIfNull(child);
        _children.Add(child);
        return child;
    }

    public override void Draw(Canvas2D canvas)
    {
        canvas.FillRoundedRect(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height, Radius, BackgroundArgb);
        canvas.DrawRect(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height, BorderArgb);
        for (int i = 0; i < _children.Count; i++)
            if (_children[i].Visible)
                _children[i].Draw(canvas);
    }
}
