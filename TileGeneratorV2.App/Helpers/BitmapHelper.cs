using System.Windows.Media;
using System.Windows.Media.Imaging;
using TileGeneratorV2.Core.Util;

namespace TileGeneratorV2.App.Helpers;

public static class BitmapHelper
{
    // Creates a BitmapSource from a PixelBuffer, scaled up by 'scale'.
    public static BitmapSource ToBitmapSource(PixelBuffer buffer, int scale = 1)
    {
        int dstW = PixelBuffer.Size * scale;
        int dstH = PixelBuffer.Size * scale;
        int stride = dstW * 4;
        var pixels = new byte[dstH * stride];

        for (int y = 0; y < dstH; y++)
        for (int x = 0; x < dstW; x++)
        {
            var c = buffer[x / scale, y / scale];
            int i = (y * dstW + x) * 4;
            pixels[i]     = c.B;
            pixels[i + 1] = c.G;
            pixels[i + 2] = c.R;
            pixels[i + 3] = c.A;
        }

        return BitmapSource.Create(dstW, dstH, 96, 96, PixelFormats.Bgra32, null, pixels, stride);
    }

    // Creates an NxN tiled grid of the buffer at 'scale' pixels per source pixel.
    public static BitmapSource ToTiledBitmapSource(PixelBuffer buffer, int tilesX, int tilesY, int scale = 1)
    {
        int srcSize = PixelBuffer.Size;
        int dstW    = srcSize * tilesX * scale;
        int dstH    = srcSize * tilesY * scale;
        int stride  = dstW * 4;
        var pixels  = new byte[dstH * stride];

        for (int y = 0; y < dstH; y++)
        for (int x = 0; x < dstW; x++)
        {
            // Wrap source coordinates across tiles
            int srcX = (x / scale) % srcSize;
            int srcY = (y / scale) % srcSize;
            var c = buffer[srcX, srcY];
            int i = (y * dstW + x) * 4;
            pixels[i]     = c.B;
            pixels[i + 1] = c.G;
            pixels[i + 2] = c.R;
            pixels[i + 3] = c.A;
        }

        return BitmapSource.Create(dstW, dstH, 96, 96, PixelFormats.Bgra32, null, pixels, stride);
    }

    // Creates a tiled grid from an array of per-tile buffers (tiles[tx, ty]).
    public static BitmapSource ToTiledBitmapSource(PixelBuffer[,] tiles, int scale = 1)
    {
        int tilesX  = tiles.GetLength(0);
        int tilesY  = tiles.GetLength(1);
        int srcSize = PixelBuffer.Size;
        int dstW    = srcSize * tilesX * scale;
        int dstH    = srcSize * tilesY * scale;
        int stride  = dstW * 4;
        var pixels  = new byte[dstH * stride];

        for (int y = 0; y < dstH; y++)
        for (int x = 0; x < dstW; x++)
        {
            int tx   = (x / scale) / srcSize;
            int ty   = (y / scale) / srcSize;
            int srcX = (x / scale) % srcSize;
            int srcY = (y / scale) % srcSize;
            var c    = tiles[tx, ty][srcX, srcY];
            int i    = (y * dstW + x) * 4;
            pixels[i]     = c.B;
            pixels[i + 1] = c.G;
            pixels[i + 2] = c.R;
            pixels[i + 3] = c.A;
        }

        return BitmapSource.Create(dstW, dstH, 96, 96, PixelFormats.Bgra32, null, pixels, stride);
    }

    // Parses a hex color string (#RRGGBB or #RGB) into ColorRgba.
    public static ColorRgba ParseHexColor(string hex)
    {
        hex = hex.TrimStart('#').Trim();
        try
        {
            if (hex.Length == 3)
                hex = $"{hex[0]}{hex[0]}{hex[1]}{hex[1]}{hex[2]}{hex[2]}";
            if (hex.Length >= 6)
            {
                byte r = Convert.ToByte(hex[..2], 16);
                byte g = Convert.ToByte(hex[2..4], 16);
                byte b = Convert.ToByte(hex[4..6], 16);
                return new ColorRgba(r, g, b);
            }
        }
        catch { /* fall through to default */ }
        return new ColorRgba(128, 128, 128);
    }

    // Converts ColorRgba to a WPF Color for UI elements.
    public static Color ToWpfColor(ColorRgba c) => Color.FromRgb(c.R, c.G, c.B);

    // Arranges a list of tiles into a grid composite bitmap.
    // gap px of dark background between cells; tiles rendered at 'scale' px per source pixel.
    public static BitmapSource ToGridBitmapSource(IReadOnlyList<PixelBuffer> tiles, int columns, int scale, int gap = 2)
    {
        int rows   = (tiles.Count + columns - 1) / columns;
        int cellW  = PixelBuffer.Size * scale + gap;
        int cellH  = PixelBuffer.Size * scale + gap;
        int dstW   = gap + columns * cellW;
        int dstH   = gap + rows    * cellH;
        int stride = dstW * 4;
        var pixels = new byte[dstH * stride];

        // Dark background (#1A1A1A)
        for (int i = 0; i < pixels.Length; i += 4)
        { pixels[i] = 0x1A; pixels[i + 1] = 0x1A; pixels[i + 2] = 0x1A; pixels[i + 3] = 0xFF; }

        for (int idx = 0; idx < tiles.Count; idx++)
        {
            int col     = idx % columns;
            int row     = idx / columns;
            int originX = gap + col * cellW;
            int originY = gap + row * cellH;
            var tile    = tiles[idx];

            int tilePixels = PixelBuffer.Size * scale;
            for (int ty = 0; ty < tilePixels; ty++)
            for (int tx = 0; tx < tilePixels; tx++)
            {
                var c  = tile[tx / scale, ty / scale];
                int pi = ((originY + ty) * dstW + (originX + tx)) * 4;
                pixels[pi]     = c.B;
                pixels[pi + 1] = c.G;
                pixels[pi + 2] = c.R;
                pixels[pi + 3] = c.A;
            }
        }

        return BitmapSource.Create(dstW, dstH, 96, 96, PixelFormats.Bgra32, null, pixels, stride);
    }
}
