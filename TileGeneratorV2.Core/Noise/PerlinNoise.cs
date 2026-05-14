namespace TileGeneratorV2.Core.Noise;

/// <summary>
/// Classic Perlin noise with 4D toroidal tileability.
/// Frequency must be a positive integer for exact tileability.
/// </summary>
public class PerlinNoise : INoiseSampler
{
    private readonly int[] _perm = new int[256];

    public PerlinNoise(int seed)
    {
        for (int i = 0; i < 256; i++) _perm[i] = i;
        var rng = new Random(seed);
        for (int i = 255; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (_perm[i], _perm[j]) = (_perm[j], _perm[i]);
        }
    }

    // Maps pixel coords to a 4D torus so left↔right and top↔bottom edges match.
    public float SampleTileable(float x, float y, int tileSize, float frequency = 4f)
        => SampleTileableXY(x, y, tileSize, frequency, frequency);

    // Anisotropic variant: different frequency in X and Y for layered/banded effects.
    public float SampleTileableXY(float x, float y, int tileSize, float freqX, float freqY)
    {
        float angleX = x / tileSize * MathF.PI * 2f;
        float angleY = y / tileSize * MathF.PI * 2f;
        // Radius = freq / (2π) ensures freq complete lattice cycles around the torus circumference
        float rx = freqX / (MathF.PI * 2f);
        float ry = freqY / (MathF.PI * 2f);
        return Noise4D(
            MathF.Cos(angleX) * rx, MathF.Sin(angleX) * rx,
            MathF.Cos(angleY) * ry, MathF.Sin(angleY) * ry
        ) * 0.5f + 0.5f;
    }

    // Fractional Brownian motion — each octave doubles frequency, halves amplitude.
    public float SampleFbm(float x, float y, int tileSize, float baseFreq = 2f, int octaves = 4)
    {
        float value = 0f, amplitude = 0.5f, totalAmp = 0f;
        for (int o = 0; o < octaves; o++)
        {
            value += SampleTileable(x, y, tileSize, baseFreq * MathF.Pow(2f, o)) * amplitude;
            totalAmp += amplitude;
            amplitude *= 0.5f;
        }
        return value / totalAmp;
    }

    // fBm with independent X/Y frequencies for anisotropic layering.
    public float SampleFbmXY(float x, float y, int tileSize, float baseFreqX, float baseFreqY, int octaves = 4)
    {
        float value = 0f, amplitude = 0.5f, totalAmp = 0f;
        for (int o = 0; o < octaves; o++)
        {
            float scale = MathF.Pow(2f, o);
            value += SampleTileableXY(x, y, tileSize, baseFreqX * scale, baseFreqY * scale) * amplitude;
            totalAmp += amplitude;
            amplitude *= 0.5f;
        }
        return value / totalAmp;
    }

    private float Noise4D(float x, float y, float z, float w)
    {
        int X = (int)MathF.Floor(x) & 255;
        int Y = (int)MathF.Floor(y) & 255;
        int Z = (int)MathF.Floor(z) & 255;
        int W = (int)MathF.Floor(w) & 255;

        x -= MathF.Floor(x); y -= MathF.Floor(y);
        z -= MathF.Floor(z); w -= MathF.Floor(w);

        float u = Fade(x), v = Fade(y), s = Fade(z), t = Fade(w);

        // Chained permutation lookups — all indices masked to [0,255]
        int h0000 = P(P(P(P(X)   + Y)   + Z)   + W);
        int h1000 = P(P(P(P(X+1) + Y)   + Z)   + W);
        int h0100 = P(P(P(P(X)   + Y+1) + Z)   + W);
        int h1100 = P(P(P(P(X+1) + Y+1) + Z)   + W);
        int h0010 = P(P(P(P(X)   + Y)   + Z+1) + W);
        int h1010 = P(P(P(P(X+1) + Y)   + Z+1) + W);
        int h0110 = P(P(P(P(X)   + Y+1) + Z+1) + W);
        int h1110 = P(P(P(P(X+1) + Y+1) + Z+1) + W);
        int h0001 = P(P(P(P(X)   + Y)   + Z)   + W+1);
        int h1001 = P(P(P(P(X+1) + Y)   + Z)   + W+1);
        int h0101 = P(P(P(P(X)   + Y+1) + Z)   + W+1);
        int h1101 = P(P(P(P(X+1) + Y+1) + Z)   + W+1);
        int h0011 = P(P(P(P(X)   + Y)   + Z+1) + W+1);
        int h1011 = P(P(P(P(X+1) + Y)   + Z+1) + W+1);
        int h0111 = P(P(P(P(X)   + Y+1) + Z+1) + W+1);
        int h1111 = P(P(P(P(X+1) + Y+1) + Z+1) + W+1);

        return Lerp(t,
            Lerp(s,
                Lerp(v, Lerp(u, G4(h0000, x,   y,   z,   w),   G4(h1000, x-1, y,   z,   w)),
                        Lerp(u, G4(h0100, x,   y-1, z,   w),   G4(h1100, x-1, y-1, z,   w))),
                Lerp(v, Lerp(u, G4(h0010, x,   y,   z-1, w),   G4(h1010, x-1, y,   z-1, w)),
                        Lerp(u, G4(h0110, x,   y-1, z-1, w),   G4(h1110, x-1, y-1, z-1, w)))),
            Lerp(s,
                Lerp(v, Lerp(u, G4(h0001, x,   y,   z,   w-1), G4(h1001, x-1, y,   z,   w-1)),
                        Lerp(u, G4(h0101, x,   y-1, z,   w-1), G4(h1101, x-1, y-1, z,   w-1))),
                Lerp(v, Lerp(u, G4(h0011, x,   y,   z-1, w-1), G4(h1011, x-1, y,   z-1, w-1)),
                        Lerp(u, G4(h0111, x,   y-1, z-1, w-1), G4(h1111, x-1, y-1, z-1, w-1)))));
    }

    private int P(int i) => _perm[i & 255];

    private static float Fade(float t) => t * t * t * (t * (t * 6f - 15f) + 10f);
    private static float Lerp(float t, float a, float b) => a + t * (b - a);

    private static float G4(int hash, float x, float y, float z, float w)
    {
        int h = hash & 31;
        float a = h < 24 ? x : y;
        float b = h < 16 ? y : z;
        float c = h < 8  ? z : w;
        return ((h & 1) != 0 ? -a : a) + ((h & 2) != 0 ? -b : b) + ((h & 4) != 0 ? -c : c);
    }
}
