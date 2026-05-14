using TileGeneratorV2.Core.Util;

namespace TileGeneratorV2.Core.Materials;

public class MaterialParameters
{
    public MaterialType Type     { get; }
    public float BaseFrequency   { get; }
    public int   Octaves         { get; }
    public ColorCurve Curve      { get; }

    public enum ColorCurve { Linear, Smooth, Stepped }

    public MaterialParameters(MaterialType type, float baseFrequency, int octaves, ColorCurve curve)
    {
        Type          = type;
        BaseFrequency = baseFrequency;
        Octaves       = octaves;
        Curve         = curve;
    }

    public static MaterialParameters FromSeed(int seed, MaterialType hint = MaterialType.Auto)
    {
        var rng = new SeededRandom(seed ^ 0xFACE);

        // All concrete types (1–13)
        var type = hint != MaterialType.Auto ? hint : (MaterialType)rng.NextInt(1, 14);

        // Marble and Wavy need integer frequencies for tileability; all others are continuous.
        float freq = type is MaterialType.Marble or MaterialType.Wavy
            ? rng.NextInt(2, 13)           // 2–12 integer
            : rng.NextFloat(2f, 14f);      // 2–14 continuous

        int octaves = rng.NextInt(3, 6);

        // Bias toward Smooth (adds natural contrast) — 60% of the time
        ColorCurve curve = rng.NextFloat() < 0.6f ? ColorCurve.Smooth : (ColorCurve)rng.NextInt(0, 3);

        return new MaterialParameters(type, freq, octaves, curve);
    }
}
