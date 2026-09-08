using System;
using SharpProspero.Graphics;

namespace MMEngine.Graphics2D;

public enum TextureFilter2D
{
    Nearest,
    /// <summary>Verified for opaque scaled blits; alpha-scaled images use the SDK's blended path.</summary>
    Smooth,
}

/// <summary>Owned PNG/TGA decode with a common native Surface view.</summary>
public sealed class Texture2D : IDisposable
{
    private readonly PngImage? _png;
    private readonly TgaImage? _tga;
    private bool _disposed;

    private Texture2D(PngImage png)
    {
        _png = png;
        Width = png.Width;
        Height = png.Height;
    }

    private Texture2D(TgaImage tga)
    {
        _tga = tga;
        Width = tga.Width;
        Height = tga.Height;
    }

    public int Width { get; }
    public int Height { get; }

    public static Texture2D FromPng(ReadOnlySpan<byte> data) => new(PngImage.Decode(data));
    public static Texture2D FromTga(ReadOnlySpan<byte> data) => new(TgaImage.Decode(data));

    public Surface AsSurface()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _png is not null ? _png.AsSurface() : _tga!.AsSurface();
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _png?.Dispose();
        _tga?.Dispose();
    }
}
