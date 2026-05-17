using TileGeneratorV2.Core.Util;

namespace TileGeneratorV2.Core.Tileset;

/// <summary>
/// Rendered tiles for one tile class, plus the metadata needed to index them.
/// Tiles are ordered to match TilesetManifest's index formula:
///   idx = variant*(CycleX*CycleY) + ym*CycleX + xm
/// </summary>
public sealed class TileGrid
{
    public IReadOnlyList<PixelBuffer> Tiles { get; }
    /// <summary>1 = plain surface fill; &gt;1 = autotile.</summary>
    public int Variants { get; }
    public int CycleX   { get; }
    public int CycleY   { get; }

    public TileGrid(IReadOnlyList<PixelBuffer> tiles, int variants, int cycleX, int cycleY)
    {
        Tiles = tiles; Variants = variants; CycleX = cycleX; CycleY = cycleY;
    }
}
