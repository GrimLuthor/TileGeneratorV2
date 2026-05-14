namespace TileGeneratorV2.Core.Noise;

/// <summary>
/// Organic contour lines: applies sin to a tileable fBm landscape.
/// Produces topographic-style rings that follow the organic topology of the
/// noise — flowing curves with no geometric structure, nothing like blobs or ridges.
/// Tileable because fBm is tileable and sin preserves periodicity.
/// </summary>
public class ContourNoise : INoiseSampler
{
    private readonly PerlinNoise _perlin;

    public ContourNoise(int seed) { _perlin = new PerlinNoise(seed); }

    public float SampleTileable(float x, float y, int tileSize, float frequency = 4f)
    {
        // Low base frequency so landscape features are large and contours are readable
        float landscape = _perlin.SampleFbm(x, y, tileSize, 2f, 5);

        // frequency controls contour density — more = tighter rings
        return MathF.Sin(landscape * frequency * 2f * MathF.PI) * 0.5f + 0.5f;
    }
}
