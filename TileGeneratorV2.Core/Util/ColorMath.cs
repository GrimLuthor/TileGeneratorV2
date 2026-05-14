namespace TileGeneratorV2.Core.Util;

public static class ColorMath
{
    public static ColorRgba Lerp(ColorRgba a, ColorRgba b, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return new ColorRgba(
            (byte)(a.R + (b.R - a.R) * t),
            (byte)(a.G + (b.G - a.G) * t),
            (byte)(a.B + (b.B - a.B) * t),
            (byte)(a.A + (b.A - a.A) * t));
    }

    public static void RgbToHsl(ColorRgba color, out float h, out float s, out float l)
    {
        float r = color.R / 255f;
        float g = color.G / 255f;
        float b = color.B / 255f;

        float max = Math.Max(r, Math.Max(g, b));
        float min = Math.Min(r, Math.Min(g, b));
        float delta = max - min;

        l = (max + min) / 2f;

        if (delta == 0f) { h = 0f; s = 0f; return; }

        s = l < 0.5f ? delta / (max + min) : delta / (2f - max - min);

        if (max == r)      h = ((g - b) / delta + (g < b ? 6f : 0f)) / 6f;
        else if (max == g) h = ((b - r) / delta + 2f) / 6f;
        else               h = ((r - g) / delta + 4f) / 6f;
    }

    public static ColorRgba HslToRgb(float h, float s, float l)
    {
        h = ((h % 1f) + 1f) % 1f;
        s = Math.Clamp(s, 0f, 1f);
        l = Math.Clamp(l, 0f, 1f);

        if (s == 0f) { byte v = (byte)(l * 255f); return new ColorRgba(v, v, v); }

        float q = l < 0.5f ? l * (1f + s) : l + s - l * s;
        float p = 2f * l - q;
        return ColorRgba.FromRgbF(HueToRgb(p, q, h + 1f / 3f), HueToRgb(p, q, h), HueToRgb(p, q, h - 1f / 3f));
    }

    private static float HueToRgb(float p, float q, float t)
    {
        t = ((t % 1f) + 1f) % 1f;
        if (t < 1f / 6f) return p + (q - p) * 6f * t;
        if (t < 1f / 2f) return q;
        if (t < 2f / 3f) return p + (q - p) * (2f / 3f - t) * 6f;
        return p;
    }

    public static ColorRgba AdjustHsl(ColorRgba color, float hShift, float sShift, float lShift)
    {
        RgbToHsl(color, out float h, out float s, out float l);
        return HslToRgb(h + hShift, Math.Clamp(s + sShift, 0f, 1f), Math.Clamp(l + lShift, 0f, 1f));
    }

    public static float Smoothstep(float t) => t * t * (3f - 2f * t);

    public static ColorRgba MultiplyBrightness(ColorRgba color, float factor)
    {
        factor = Math.Max(0f, factor);
        return ColorRgba.FromRgbF(
            Math.Min(1f, color.R / 255f * factor),
            Math.Min(1f, color.G / 255f * factor),
            Math.Min(1f, color.B / 255f * factor),
            color.A / 255f);
    }
}
