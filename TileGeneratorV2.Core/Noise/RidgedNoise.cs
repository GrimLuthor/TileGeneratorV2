namespace TileGeneratorV2.Core.Noise;

/// <summary>
/// Ridged multifractal noise: sharp bright ridges on a dark field.
/// Mimics rock strata, mountain ridges, and eroded stone surfaces.
/// Higher octaves are weighted by the sharpness of lower octaves.
/// </summary>
public class RidgedNoise : INoiseSampler
{
    private readonly PerlinNoise _perlin;
    private readonly int _octaves;

    public RidgedNoise(int seed, int octaves = 5)
    {
        _perlin = new PerlinNoise(seed);
        _octaves = octaves;
    }

    public float SampleTileable(float x, float y, int tileSize, float frequency = 4f)
    {
        float accum = 0f, amplitude = 0.5f, totalAmp = 0f;
        float weight = 1f;

        for (int o = 0; o < _octaves; o++)
        {
            float n = _perlin.SampleTileable(x, y, tileSize, frequency * MathF.Pow(2f, o));
            // Fold: 1 at midpoint (ridge peak), 0 at extremes
            n = 1f - MathF.Abs(n * 2f - 1f);
            n *= n * weight;           // sharpen and weight by parent sharpness
            weight = Math.Clamp(n * 2f, 0f, 1f);

            accum    += n * amplitude;
            totalAmp += amplitude;
            amplitude *= 0.5f;
        }

        return Math.Clamp(accum / totalAmp, 0f, 1f);
    }
}
