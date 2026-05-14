namespace TileGeneratorV2.Core.Noise;

/// <summary>
/// Turbulence noise: sum of absolute values of fBm octaves.
/// Produces organic cloud/smoke-like patterns. Inverted so smooth regions
/// are bright and turbulent/chaotic regions are dark.
/// </summary>
public class TurbulenceNoise : INoiseSampler
{
    private readonly PerlinNoise _perlin;
    private readonly int _octaves;

    public TurbulenceNoise(int seed, int octaves = 4)
    {
        _perlin = new PerlinNoise(seed);
        _octaves = octaves;
    }

    public float SampleTileable(float x, float y, int tileSize, float frequency = 4f)
    {
        float accum = 0f, amplitude = 1f, totalAmp = 0f;

        for (int o = 0; o < _octaves; o++)
        {
            float n = _perlin.SampleTileable(x, y, tileSize, frequency * MathF.Pow(2f, o));
            // Centre around 0, take abs — creases at 0.5 become valleys
            accum += MathF.Abs(n - 0.5f) * 2f * amplitude;
            totalAmp += amplitude;
            amplitude *= 0.5f;
        }

        // Invert: smooth regions (low turbulence) map to 1, creased regions to 0
        return 1f - Math.Clamp(accum / totalAmp, 0f, 1f);
    }
}
