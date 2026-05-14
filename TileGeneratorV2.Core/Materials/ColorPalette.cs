using TileGeneratorV2.Core.Util;

namespace TileGeneratorV2.Core.Materials;

public class ColorPalette
{
    public float Hue1       { get; }  // [0, 1]
    public float Hue2       { get; }  // [0, 1]
    public float Saturation { get; }  // [0, 1]
    public float LightnessLight { get; }  // bright end [0, 1]
    public float LightnessDark  { get; }  // dark end  [0, 1]

    public ColorPalette(float hue1, float hue2, float saturation, float lightnessLight, float lightnessDark)
    {
        Hue1           = hue1;
        Hue2           = hue2;
        Saturation     = saturation;
        LightnessLight = lightnessLight;
        LightnessDark  = lightnessDark;
    }

    // t=0 → dark end, t=1 → light end
    public ColorRgba Sample(float t)
        => ColorMath.HslToRgb(LerpHue(t), Saturation, LerpLit(t));

    public ColorRgba SampleSmooth(float t)
        => Sample(ColorMath.Smoothstep(t));

    public ColorRgba SampleStepped(float t, int steps = 4)
    {
        t = Math.Clamp(MathF.Floor(t * steps) / (steps - 1), 0f, 1f);
        return Sample(t);
    }

    // Shortest-path hue interpolation around the colour wheel
    private float LerpHue(float t)
    {
        float diff = ((Hue2 - Hue1 + 1.5f) % 1f) - 0.5f;
        return ((Hue1 + diff * t) % 1f + 1f) % 1f;
    }

    private float LerpLit(float t) => LightnessDark + (LightnessLight - LightnessDark) * t;

    // Default warm stone palette (single hue, wide darkness range)
    public static ColorPalette WarmStone() => new(
        hue1:           0.083f,   // ~30° golden-tan
        hue2:           0.072f,   // ~26° slight shift toward brown
        saturation:     0.48f,
        lightnessLight: 0.62f,
        lightnessDark:  0.14f);
}
