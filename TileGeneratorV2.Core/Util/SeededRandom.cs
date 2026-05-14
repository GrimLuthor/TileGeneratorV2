namespace TileGeneratorV2.Core.Util;

public class SeededRandom
{
    private readonly Random _rng;
    private readonly int _seed;

    public int Seed => _seed;

    public SeededRandom(int seed) { _seed = seed; _rng = new Random(seed); }

    public float NextFloat() => (float)_rng.NextDouble();
    public float NextFloat(float min, float max) => min + NextFloat() * (max - min);
    public int NextInt(int min, int max) => _rng.Next(min, max);
    public bool NextBool(float probability = 0.5f) => NextFloat() < probability;

    public SeededRandom Fork(int subSeed) => new(_seed ^ unchecked(subSeed * 1013904223));

    // Stateless hash functions — same inputs always return same outputs, no state advance

    public static float Hash1(int seed, int x)
    {
        uint h = (uint)(seed ^ unchecked(x * 374761393));
        h = (h ^ (h >> 13)) * 1274126177u;
        h ^= h >> 16;
        return (h & 0xFFFFFF) / (float)0x1000000;
    }

    public static (float X, float Y) Hash2(int seed, int x, int y)
    {
        return (Hash1(seed ^ unchecked((int)0xDEADBEEF), unchecked(x * 1664525 + y * 1013904223)),
                Hash1(seed ^ unchecked((int)0xCAFEBABE), unchecked(x * 1013904223 + y * 1664525)));
    }

    public static (float X, float Y, float Z) Hash3(int seed, int id)
    {
        return (Hash1(seed ^ 0x12345678, id),
                Hash1(seed ^ unchecked((int)0x87654321), id),
                Hash1(seed ^ unchecked((int)0xABCDEF01), id));
    }
}
