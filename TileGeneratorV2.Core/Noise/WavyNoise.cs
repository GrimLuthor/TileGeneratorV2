namespace TileGeneratorV2.Core.Noise;

/// <summary>
/// Two orthogonal sine waves domain-warped by Perlin, then nested inside a
/// second sine — sin((sin(x·f) + sin(y·f)) · π). Produces organic ripple /
/// channel interference patterns. Tileable because sine frequency is integer
/// and the Perlin warp is itself tileable.
/// </summary>
public class WavyNoise : INoiseSampler
{
    private readonly PerlinNoise _perlin;

    public WavyNoise(int seed) { _perlin = new PerlinNoise(seed); }

    public float SampleTileable(float x, float y, int tileSize, float frequency = 4f)
    {
        float freq  = MathF.Max(1f, MathF.Round(frequency));
        float scale = 2f * MathF.PI / tileSize;

        // Tileable domain warp — low-frequency Perlin offset
        float warpStrength = tileSize * 0.12f;
        float wx = (_perlin.SampleTileable(x,                      y,                      tileSize, 2f) - 0.5f) * warpStrength;
        float wy = (_perlin.SampleTileable(x + tileSize * 0.37f,   y + tileSize * 0.57f,   tileSize, 2f) - 0.5f) * warpStrength;

        float w1 = MathF.Sin((x + wx) * scale * freq);
        float w2 = MathF.Sin((y + wy) * scale * freq);

        // Nested sine: turns two smooth waves into dense interference channels
        return MathF.Sin((w1 + w2) * MathF.PI) * 0.5f + 0.5f;
    }
}
