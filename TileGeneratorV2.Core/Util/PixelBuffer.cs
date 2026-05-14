namespace TileGeneratorV2.Core.Util;

public class PixelBuffer
{
    public const int Size = 32;
    private readonly ColorRgba[,] _pixels = new ColorRgba[Size, Size];

    public ColorRgba this[int x, int y]
    {
        get => _pixels[x, y];
        set => _pixels[x, y] = value;
    }

    public void Fill(ColorRgba color)
    {
        for (int y = 0; y < Size; y++)
        for (int x = 0; x < Size; x++)
            _pixels[x, y] = color;
    }

    // Returns BGRA bytes (WPF WriteableBitmap / BitmapSource format)
    public byte[] ToBgraBytes()
    {
        var bytes = new byte[Size * Size * 4];
        for (int y = 0; y < Size; y++)
        for (int x = 0; x < Size; x++)
        {
            var c = _pixels[x, y];
            int i = (y * Size + x) * 4;
            bytes[i]     = c.B;
            bytes[i + 1] = c.G;
            bytes[i + 2] = c.R;
            bytes[i + 3] = c.A;
        }
        return bytes;
    }

    public PixelBuffer Clone()
    {
        var copy = new PixelBuffer();
        for (int y = 0; y < Size; y++)
        for (int x = 0; x < Size; x++)
            copy._pixels[x, y] = _pixels[x, y];
        return copy;
    }
}
