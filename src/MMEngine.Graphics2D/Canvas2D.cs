using System;
using MMEngine.Core;
using SharpProspero.Graphics;

namespace MMEngine.Graphics2D;

/// <summary>Immediate 2D API over SharpProspero's native framebuffer Surface.</summary>
public readonly struct Canvas2D(Surface surface, EngineDiagnostics? diagnostics = null)
{
    private readonly EngineDiagnostics? _diagnostics = diagnostics;

    public Surface Surface { get; } = surface;
    public int Width => Surface.Width;
    public int Height => Surface.Height;

    public void Clear(uint argb)
    {
        Surface.Clear(new Color(argb));
        CountDraw();
    }

    public void FillRect(int x, int y, int width, int height, uint argb)
    {
        Surface.FillRect(x, y, width, height, new Color(argb));
        CountDraw();
    }

    public void DrawRect(int x, int y, int width, int height, uint argb)
    {
        Surface.DrawRect(x, y, width, height, new Color(argb));
        CountDraw();
    }

    public void FillRoundedRect(int x, int y, int width, int height, int radius, uint argb)
    {
        Surface.FillRoundedRect(x, y, width, height, radius, new Color(argb));
        CountDraw();
    }

    public void DrawLine(int x0, int y0, int x1, int y1, uint argb, int thickness = 1)
    {
        Surface.DrawLine(x0, y0, x1, y1, new Color(argb), thickness);
        CountDraw();
    }

    public void DrawTexture(Texture2D texture, int x, int y, int width = 0, int height = 0,
        bool alphaBlend = true, TextureFilter2D filter = TextureFilter2D.Nearest)
    {
        ArgumentNullException.ThrowIfNull(texture);
        Surface source = texture.AsSurface();
        int targetWidth = width <= 0 ? texture.Width : width;
        int targetHeight = height <= 0 ? texture.Height : height;
        if (targetWidth == texture.Width && targetHeight == texture.Height)
        {
            if (alphaBlend)
                Surface.BlitBlended(source, x, y);
            else
                Surface.Blit(source, x, y);
            CountDraw();
            return;
        }
        if (filter == TextureFilter2D.Smooth && !alphaBlend)
            Surface.BlitScaledSmooth(source, x, y, targetWidth, targetHeight);
        else if (alphaBlend)
            Surface.BlitScaledBlended(source, x, y, targetWidth, targetHeight);
        else
            Surface.BlitScaled(source, x, y, targetWidth, targetHeight);
        CountDraw();
    }

    /// <summary>Draws a stretchable image panel while preserving its corner pixels.</summary>
    public void DrawNineSlice(Texture2D texture, int x, int y, int width, int height, int border)
    {
        ArgumentNullException.ThrowIfNull(texture);
        Surface.BlitNineSlice(texture.AsSurface(), x, y, width, height, border);
        CountDraw();
    }

    public void DrawText(Font2D font, string text, int x, int y, uint argb)
    {
        ArgumentNullException.ThrowIfNull(font);
        font.NativeFont.DrawText(Surface, text, x, y, new Color(argb));
        CountDraw();
    }

    private void CountDraw()
    {
        if (_diagnostics is not null)
            _diagnostics.DrawCalls2D++;
    }
}
