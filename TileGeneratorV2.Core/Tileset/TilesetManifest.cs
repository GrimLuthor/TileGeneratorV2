using TileGeneratorV2.Core.Util;

namespace TileGeneratorV2.Core.Tileset;

/// <summary>
/// Serializable description of an exported tileset. Consumers read each class's cycle
/// values and apply <see cref="IndexFormula"/> to map (variant, worldX, worldY) onto an
/// atlas cell — no per-tile lookup table is needed because the cycle is uniform per class.
/// </summary>
public sealed class TilesetManifest
{
    public int Seed     { get; set; }
    public int TileSize { get; set; } = PixelBuffer.Size;

    public string IndexFormula { get; set; } =
        "idx = variant*(cycleX*cycleY) + (worldY%cycleY)*cycleX + (worldX%cycleX); " +
        "atlasCol = idx % atlasColumns; atlasRow = idx / atlasColumns";

    public TileClass? Cap   { get; set; }
    public TileClass? Wall  { get; set; }
    public TileClass? Floor { get; set; }

    /// <summary>One tile class (cap / wall / floor) within an exported tileset.</summary>
    public sealed class TileClass
    {
        public string Atlas        { get; set; } = "";
        /// <summary>1 = plain surface fill; &gt;1 = autotile (variant chosen from neighbours).</summary>
        public int    Variants     { get; set; }
        public int    CycleX       { get; set; }
        public int    CycleY       { get; set; }
        /// <summary>Atlas width in tiles; the atlas is a row-major grid.</summary>
        public int    AtlasColumns { get; set; }
    }
}
