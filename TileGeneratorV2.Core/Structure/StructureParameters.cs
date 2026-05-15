using TileGeneratorV2.Core.Util;

namespace TileGeneratorV2.Core.Structure;

public class StructureParameters
{
    public BondType Bond               { get; }
    public int      BrickWidth         { get; }   // px
    public int      BrickHeight        { get; }   // px
    public float    MortarWidth        { get; }   // px (each brick contributes MortarWidth/2 on each side)
    public float    WeatheringStrength { get; }   // 0 = fresh, 1 = heavily weathered
    public int      Seed               { get; }   // kept for per-row ashlar hashing

    public StructureParameters(BondType bond, int brickWidth, int brickHeight, float mortarWidth, float weatheringStrength, int seed)
    {
        Bond               = bond;
        BrickWidth         = brickWidth;
        BrickHeight        = brickHeight;
        MortarWidth        = mortarWidth;
        WeatheringStrength = weatheringStrength;
        Seed               = seed;
    }

    public static StructureParameters FromSeed(int seed)
    {
        var rng = new SeededRandom(seed ^ unchecked((int)0x577EC700));

        var   bond       = (BondType)rng.NextInt(0, 3);
        int   brickH     = rng.NextInt(4, 23);                                  // 4–22 px
        int   brickW     = rng.NextInt(brickH, Math.Min(brickH * 3 + 1, 30));  // H to 3H, ≤ 30
        float mortarW    = rng.NextFloat(1.0f, 2.8f);
        float weathering = rng.NextFloat(0f, 1f);

        return new StructureParameters(bond, brickW, brickH, mortarW, weathering, seed);
    }
}
