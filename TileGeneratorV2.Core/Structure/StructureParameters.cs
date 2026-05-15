using TileGeneratorV2.Core.Util;

namespace TileGeneratorV2.Core.Structure;

public class StructureParameters
{
    public BondType        Bond               { get; }
    public int             BrickWidth         { get; }   // px
    public int             BrickHeight        { get; }   // px
    public float           MortarWidth        { get; }   // px (each brick contributes MortarWidth/2 on each side)
    public float           WeatheringStrength { get; }   // 0 = fresh, 1 = heavily weathered (seed-derived)
    public WeatheringPreset Preset            { get; }
    public int             Seed               { get; }   // kept for per-row ashlar hashing

    // How much weathering effects (stains, groove dirt) are applied
    public float EffectiveWeathering => Preset switch
    {
        WeatheringPreset.Clean   => WeatheringStrength * 0.15f,
        WeatheringPreset.Broken  => 0.4f + WeatheringStrength * 0.6f,
        _                        => WeatheringStrength
    };

    // How aggressively brick edges are chipped
    public float ErosionStrength => Preset switch
    {
        WeatheringPreset.Clean    => 0f,
        WeatheringPreset.Weathered => WeatheringStrength * 0.5f,
        WeatheringPreset.Broken   => 0.4f + WeatheringStrength * 0.6f,
        _                         => 0f
    };

    public StructureParameters(BondType bond, int brickWidth, int brickHeight, float mortarWidth,
                                float weatheringStrength, WeatheringPreset preset, int seed)
    {
        Bond               = bond;
        BrickWidth         = brickWidth;
        BrickHeight        = brickHeight;
        MortarWidth        = mortarWidth;
        WeatheringStrength = weatheringStrength;
        Preset             = preset;
        Seed               = seed;
    }

    public static StructureParameters FromSeed(int seed, WeatheringPreset preset = WeatheringPreset.Weathered)
    {
        var rng = new SeededRandom(seed ^ unchecked((int)0x577EC700));

        var   bond       = (BondType)rng.NextInt(0, 3);
        int   brickH     = rng.NextInt(4, 23);                                  // 4–22 px
        int   brickW     = rng.NextInt(brickH, Math.Min(brickH * 3 + 1, 30));  // H to 3H, ≤ 30
        float mortarW    = rng.NextFloat(1.0f, 2.8f);
        float weathering = rng.NextFloat(0f, 1f);

        return new StructureParameters(bond, brickW, brickH, mortarW, weathering, preset, seed);
    }
}
