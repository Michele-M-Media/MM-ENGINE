using System;
using SharpProspero.Graphics;

namespace MMEngine.Graphics2D;

/// <summary>Antialiased TrueType text with a built-in bitmap fallback.</summary>
public sealed class Font2D : IDisposable
{
    private readonly IDisposable? _owned;

    private Font2D(ITextFont font, IDisposable? owned)
    {
        NativeFont = font;
        _owned = owned;
    }

    public ITextFont NativeFont { get; }
    public int LineHeight => NativeFont.LineHeight;

    public static Font2D FromTrueType(ReadOnlySpan<byte> data, float pixelSize = 24f)
    {
        TrueTypeFont font = TrueTypeFont.Load(data, pixelSize);
        return new Font2D(font, font);
    }

    public static Font2D Bitmap(int scale = 2) => new(new BitmapTextFont(scale), null);
    public int Measure(string text) => NativeFont.MeasureText(text);
    public void Dispose() => _owned?.Dispose();
}
