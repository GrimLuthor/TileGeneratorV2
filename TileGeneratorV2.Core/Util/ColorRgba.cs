namespace TileGeneratorV2.Core.Util;

public readonly struct ColorRgba
{
    public byte R { get; }
    public byte G { get; }
    public byte B { get; }
    public byte A { get; }

    public ColorRgba(byte r, byte g, byte b, byte a = 255)
    {
        R = r; G = g; B = b; A = a;
    }

    public static readonly ColorRgba Black = new(0, 0, 0);
    public static readonly ColorRgba White = new(255, 255, 255);

    public static ColorRgba FromRgbF(float r, float g, float b, float a = 1f) =>
        new((byte)Math.Clamp((int)(r * 255f), 0, 255),
            (byte)Math.Clamp((int)(g * 255f), 0, 255),
            (byte)Math.Clamp((int)(b * 255f), 0, 255),
            (byte)Math.Clamp((int)(a * 255f), 0, 255));

    public (float R, float G, float B) ToRgbF() => (R / 255f, G / 255f, B / 255f);

    public override string ToString() => $"#{R:X2}{G:X2}{B:X2}";
}
