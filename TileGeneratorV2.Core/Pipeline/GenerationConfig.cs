using TileGeneratorV2.Core.Materials;
using TileGeneratorV2.Core.Util;

namespace TileGeneratorV2.Core.Pipeline;

/// <summary>
/// Master configuration for a full tileset generation run.
/// Seed + palette drive all randomized choices; overrides let the user pin specific params.
/// </summary>
public class GenerationConfig
{
    public int Seed { get; set; }
    public ColorPalette Palette { get; set; } = ColorPalette.WarmStone();

    // Null = derive from seed automatically
    public MaterialParameters? MaterialOverride { get; set; }

    public GenerationConfig() { }

    public GenerationConfig(int seed, ColorPalette palette, MaterialParameters? materialOverride = null)
    {
        Seed             = seed;
        Palette          = palette;
        MaterialOverride = materialOverride;
    }
}
