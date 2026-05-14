using TileGeneratorV2.Core.Util;

namespace TileGeneratorV2.Core.Noise;

/// <summary>
/// Directional marble: linear gradient in a seed-driven vein direction, warped
/// by tileable turbulence. xFreq and yFreq are always even so the linear term
/// completes a whole number of half-cycles across the tile, preserving seamless tiling.
/// turbSmooth controls whether the warp uses linear or smoothstep interpolation.
/// </summary>
public class MarbleNoise : INoiseSampler
{
    private readonly PerlinNoise _perlin;
    private readonly int   _xFreq;       // even int [0,8]
    private readonly int   _yFreq;       // even int [0,8]
    private readonly float _turbPower;   // warp strength
    private readonly bool  _turbSmooth;  // smoothstep vs linear warp

    public MarbleNoise(int seed)
    {
        _perlin = new PerlinNoise(seed);
        var rng = new SeededRandom(seed ^ unchecked((int)0xABCD1234));

        int[] even = { 0, 2, 4, 6, 8 };
        _xFreq = even[rng.NextInt(0, even.Length)];
        _yFreq = even[rng.NextInt(0, even.Length)];
        if (_xFreq == 0 && _yFreq == 0) _xFreq = 2;

        _turbPower  = rng.NextFloat(1.5f, 6f);
        _turbSmooth = rng.NextBool();
    }

    public float SampleTileable(float x, float y, int tileSize, float frequency = 4f)
    {
        // Directional base: even xFreq/yFreq * integer frequency → seamlessly tileable
        float linear = x * _xFreq / tileSize + y * _yFreq / tileSize;

        // Tileable turbulence (fBm), optionally smoothstepped per-octave
        float turb = ComputeTurb(x, y, tileSize, MathF.Max(2f, frequency * 0.5f), 4);

        // abs(sin) → thin dark veins on bright stone background
        return MathF.Abs(MathF.Sin((linear + _turbPower * turb) * MathF.PI * frequency));
    }

    private float ComputeTurb(float x, float y, int tileSize, float baseFreq, int octaves)
    {
        float accum = 0f, amp = 1f, total = 0f;
        for (int o = 0; o < octaves; o++)
        {
            float n = _perlin.SampleTileable(x, y, tileSize, baseFreq * MathF.Pow(2f, o));
            if (_turbSmooth) n = n * n * (3f - 2f * n); // smoothstep
            accum += n * amp;
            total += amp;
            amp   *= 0.5f;
        }
        return accum / total;
    }
}
