using System.Linq;
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
    public int             Seed               { get; }   // kept for per-row ashlar/flagstone hashing
    public bool            IsFloor            => Bond >= BondType.StoneSlab;

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

    // Brick dims are restricted to {2^k} ∪ {3·2^k} so their odd part — the brick
    // pattern's tile-cycle — is always 1 or 3, keeping the autotile set bounded.
    private static readonly int[] BrickHeights = { 4, 6, 8, 12, 16 };
    private static readonly int[] BrickWidths  = { 4, 6, 8, 12, 16, 24 };

    public static StructureParameters FromSeed(int seed, WeatheringPreset preset = WeatheringPreset.Weathered)
    {
        var rng = new SeededRandom(seed ^ unchecked((int)0x577EC700));

        var bond   = (BondType)rng.NextInt(0, 3);                  // wall bonds only
        int brickH = BrickHeights[rng.NextInt(0, BrickHeights.Length)];

        // width: brick-shaped — between H and 3H, from the allowed set
        var wOpts  = BrickWidths.Where(w => w >= brickH && w <= brickH * 3).ToArray();
        int brickW = wOpts[rng.NextInt(0, wOpts.Length)];

        float mortarW    = rng.NextFloat(1.0f, 2.8f);
        float weathering = rng.NextFloat(0f, 1f);

        return new StructureParameters(bond, brickW, brickH, mortarW, weathering, preset, seed);
    }

    /// <summary>Odd part of n — n with every factor of 2 removed.</summary>
    public static int OddPart(int n)
    {
        if (n < 1) return 1;
        while ((n & 1) == 0) n >>= 1;
        return n;
    }

    /// <summary>
    /// Cap-atlas tile cycle for this wall. Caps render the brick-width pattern along
    /// every rim, so a cap realigns every CapCycle tiles on both axes (1 = self-tiling, 3).
    /// </summary>
    public int CapCycle => OddPart(BrickWidth);

    public static StructureParameters FromSeedFloor(int seed, WeatheringPreset preset = WeatheringPreset.Weathered)
    {
        var rng = new SeededRandom(seed ^ unchecked((int)0x8A3F1200));

        // Weighted: BigSlab 25%, SquareGrid 25%, StoneSlab 25%, Flagstone 12.5%, Cobblestone 12.5%
        var bond = rng.NextInt(0, 8) switch
        {
            0 or 1 => BondType.BigSlab,
            2 or 3 => BondType.SquareGrid,
            4 or 5 => BondType.StoneSlab,
            6      => BondType.Flagstone,
            _      => BondType.Cobblestone,
        };

        int brickH, brickW;
        float mortarW;
        switch (bond)
        {
            case BondType.StoneSlab:
                brickH  = rng.NextInt(10, 17);   // 10–16px, squarish
                brickW  = rng.NextInt(12, 21);   // 12–20px
                mortarW = rng.NextFloat(0.8f, 1.8f);
                break;
            case BondType.Flagstone:
                brickH  = rng.NextInt(8, 14);    // base height, varies per row
                brickW  = rng.NextInt(8, 16);    // base width, varies per col
                mortarW = rng.NextFloat(1.0f, 2.5f);
                break;
            case BondType.BigSlab:
                brickH  = PixelBuffer.Size;      // unused for rendering, set for display
                brickW  = PixelBuffer.Size;
                mortarW = rng.NextFloat(3.0f, 7.0f);
                break;
            case BondType.SquareGrid:
                int sq  = rng.NextInt(0, 2) == 0 ? 8 : 16;  // 4×4 or 2×2 grid
                brickH  = sq;
                brickW  = sq;
                mortarW = rng.NextFloat(2.0f, 5.0f);
                break;
            default: // Cobblestone
                brickH  = rng.NextInt(4, 8);     // 4–7px
                brickW  = rng.NextInt(4, 9);     // 4–8px
                mortarW = rng.NextFloat(1.5f, 3.0f);
                break;
        }

        float weathering = rng.NextFloat(0f, 1f);
        return new StructureParameters(bond, brickW, brickH, mortarW, weathering, preset, seed);
    }
}
