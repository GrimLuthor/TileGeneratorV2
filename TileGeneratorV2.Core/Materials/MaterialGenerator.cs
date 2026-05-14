using TileGeneratorV2.Core.Noise;
using TileGeneratorV2.Core.Util;

namespace TileGeneratorV2.Core.Materials;

/// <summary>
/// Stage 1: Generates a seamlessly tileable 32×32 material texture.
/// No knowledge of bricks, edges, or tile position.
/// </summary>
public class MaterialGenerator
{
    public PixelBuffer Generate(int seed, ColorPalette palette, MaterialParameters? parameters = null)
    {
        var p = parameters ?? MaterialParameters.FromSeed(seed);

        var noise = CreateNoiseSampler(p, seed);

        var buffer   = new PixelBuffer();
        int tileSize = PixelBuffer.Size;

        for (int py = 0; py < tileSize; py++)
        for (int px = 0; px < tileSize; px++)
        {
            float val = SampleWithType(noise, p, px, py, tileSize);

            // Boost contrast so the full primary→secondary range is used
            val = BoostContrast(val, 1.8f);

            ColorRgba color = p.Curve switch
            {
                MaterialParameters.ColorCurve.Smooth  => palette.SampleSmooth(val),
                MaterialParameters.ColorCurve.Stepped => palette.SampleStepped(val),
                _                                     => palette.Sample(val)
            };

            buffer[px, py] = color;
        }

        return buffer;
    }

    // Pushes values away from 0.5 toward 0 and 1 without clamping the middle
    private static float BoostContrast(float t, float factor)
        => Math.Clamp((t - 0.5f) * factor + 0.5f, 0f, 1f);

    private static float SampleWithType(INoiseSampler noise, MaterialParameters p, int px, int py, int tileSize)
    {
        switch (p.Type)
        {
            case MaterialType.Layered when noise is PerlinNoise perlin:
                // Horizontal bands: X-frequency much lower than Y-frequency
                return perlin.SampleFbmXY(px, py, tileSize, p.BaseFrequency / 3f, p.BaseFrequency, p.Octaves);

            case MaterialType.Rough when noise is PerlinNoise perlinRough:
            {
                // Domain warp: offset sample coordinates using a second noise layer
                float warpScale = tileSize * 0.3f;
                float warpX = (perlinRough.SampleTileable(px, py,                        tileSize, p.BaseFrequency) - 0.5f) * warpScale;
                float warpY = (perlinRough.SampleTileable(px + tileSize * 0.37f,
                                                          py + tileSize * 0.57f,          tileSize, p.BaseFrequency) - 0.5f) * warpScale;
                return perlinRough.SampleFbm(px + warpX, py + warpY, tileSize, p.BaseFrequency, p.Octaves);
            }

            case MaterialType.Granite when noise is PerlinNoise granite:
                // High frequency, many octaves for a speckled look
                return granite.SampleFbm(px, py, tileSize, p.BaseFrequency * 2f, Math.Max(p.Octaves, 5));

            case MaterialType.SmoothStone when noise is PerlinNoise smooth:
                // Low frequency, few octaves — very gradual variation
                return smooth.SampleFbm(px, py, tileSize, MathF.Max(2f, p.BaseFrequency / 2f), 2);

            default:
            {
                if (noise is PerlinNoise perlinFbm)
                    return perlinFbm.SampleFbm(px, py, tileSize, p.BaseFrequency, p.Octaves);
                return noise.SampleTileable(px, py, tileSize, p.BaseFrequency);
            }
        }
    }

    private static INoiseSampler CreateNoiseSampler(MaterialParameters p, int seed)
        => p.Type == MaterialType.Composite
            ? CreateCompositeSampler(seed)
            : CreateSingleSampler(p.Type, seed);

    // Creates a sampler for any non-composite type; used both directly and by composite layers.
    private static INoiseSampler CreateSingleSampler(MaterialType type, int seed)
    {
        var rng = new SeededRandom(seed ^ 0x1234);
        float crackWidth = rng.NextFloat(0.12f, 0.28f);

        return type switch
        {
            MaterialType.Cobblestone => new VoronoiNoise(seed, cellBlend: 0.65f),
            MaterialType.Marble      => new MarbleNoise(seed),
            MaterialType.Cracked     => new CrackedNoise(seed, crackWidth),
            MaterialType.Cellular    => new CellularNoise(seed),
            MaterialType.Wavy        => new WavyNoise(seed),
            MaterialType.Ridged      => new RidgedNoise(seed, octaves: 5),
            MaterialType.Wood        => new WoodNoise(seed),
            MaterialType.Contour     => new ContourNoise(seed),
            _                        => new PerlinNoise(seed)
        };
    }

    private static readonly MaterialType[] _compositeCandidates =
    {
        MaterialType.Cobblestone, MaterialType.Marble,    MaterialType.Granite,
        MaterialType.SmoothStone, MaterialType.Rough,     MaterialType.Layered,
        MaterialType.Cracked,     MaterialType.Cellular,  MaterialType.Wavy,
        MaterialType.Ridged,      MaterialType.Wood,      MaterialType.Contour,
    };

    private static INoiseSampler CreateCompositeSampler(int seed)
    {
        var rng = new SeededRandom(seed ^ unchecked((int)0xC0A1B2C3));

        int ia = rng.NextInt(0, _compositeCandidates.Length);
        int ib;
        do { ib = rng.NextInt(0, _compositeCandidates.Length); } while (ib == ia);

        // Layer A: coarser — gives the macro structural character
        // Layer B: finer  — gives the micro surface detail
        float freqA      = rng.NextFloat(3f,   7f);
        float freqB      = rng.NextFloat(7f,  14f);
        float macroFreq  = rng.NextFloat(2f,   3f);
        float threshold  = rng.NextFloat(0.35f, 0.65f);
        // 40% hard contact, 60% soft gradual transition
        float blendWidth = rng.NextBool(0.4f) ? 0f : rng.NextFloat(0.08f, 0.22f);

        var layerA = CreateSingleSampler(_compositeCandidates[ia], seed ^ unchecked((int)0x11111111));
        var layerB = CreateSingleSampler(_compositeCandidates[ib], seed ^ unchecked((int)0x22222222));

        return new CompositeNoise(layerA, freqA, layerB, freqB, seed, macroFreq, threshold, blendWidth);
    }
}
