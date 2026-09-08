using System;
using System.Globalization;
using System.IO;
using SharpProspero.Texture;

namespace MMEngine.TextureConverter;

internal static class Program
{
    private static int Main(string[] args)
    {
        try
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: mmtexture <input.png|tga|bmp|qoi> <output.gnf> [--srgb] [--resize WIDTHxHEIGHT] [--flip-v]");
                return args.Length == 0 ? 0 : 2;
            }
            bool srgb = false;
            bool flipVertical = false;
            int width = 0, height = 0;
            for (int i = 2; i < args.Length; i++)
            {
                if (args[i] == "--srgb")
                    srgb = true;
                else if (args[i] == "--flip-v")
                    flipVertical = true;
                else if (args[i] == "--resize" && i + 1 < args.Length)
                {
                    string[] size = args[++i].Split('x', 'X');
                    if (size.Length != 2 || !int.TryParse(size[0], NumberStyles.None, CultureInfo.InvariantCulture, out width)
                        || !int.TryParse(size[1], NumberStyles.None, CultureInfo.InvariantCulture, out height)
                        || width <= 0 || height <= 0)
                        throw new ArgumentException("Resize must be WIDTHxHEIGHT with positive integers.");
                }
                else
                    throw new ArgumentException($"Unknown option '{args[i]}'.");
            }

            DecodedImage image = DecodedImage.Load(args[0]);
            if (width > 0)
                image = ImageOps.Resize(image, width, height);
            if (flipVertical)
                image = ImageOps.FlipVertical(image);
            byte[] gnf = GnfWriter.Build(image, srgb);
            GnfInfo info = GnfReader.Read(gnf);
            int expectedFormat = srgb ? 130 : 56;
            if (info.Version != 4 || info.TextureCount != 1 || info.Alignment != 256 || info.TileMode != 0
                || info.DataFormat != expectedFormat || info.PixelSize <= 0
                || info.Width != image.Width || info.Height != image.Height || info.StreamSize != gnf.Length)
                throw new InvalidOperationException("Generated GNF did not pass read-back validation.");
            File.WriteAllBytes(args[1], gnf);
            Console.WriteLine($"Wrote {args[1]}: {info.Width}x{info.Height}, {gnf.Length} bytes, linear, sRGB={srgb}.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"mmtexture: {exception.Message}");
            return 1;
        }
    }
}
