using TileGeneratorV2.Core.Context;
using TileGeneratorV2.Core.Materials;
using TileGeneratorV2.Core.Structure;
using TileGeneratorV2.Core.Util;

namespace TileGeneratorV2.Core.Tileset;

/// <summary>
/// Builds the complete cap-tile set for one seed: every blob-autotile variant rendered
/// at every world-position phase of the brick cycle.
///
/// Variant order is the cardinal wall-neighbour mask  variant = N | E&lt;&lt;1 | S&lt;&lt;2 | W&lt;&lt;3
/// (bit set = that neighbour is a wall). Tiles are emitted in index-formula order
///   idx = variant*(cycle*cycle) + ym*cycle + xm
/// so a consumer maps (variant, worldX, worldY) straight to a tile — no table.
/// </summary>
public static class CapTilesetBuilder
{
    public const int VariantCount = 16;   // one per cardinal wall-neighbour combination

    public static TileGrid Build(
        int seed, ColorPalette palette, MaterialParameters? matParams, StructureParameters wallParams)
    {
        int cycle = wallParams.CapCycle;   // 1 (self-tiling) or 3
        var tiles = new List<PixelBuffer>(VariantCount * cycle * cycle);

        for (int variant = 0; variant < VariantCount; variant++)
        {
            var flags = MaskToFlags(variant);
            for (int ym = 0; ym < cycle; ym++)
            for (int xm = 0; xm < cycle; xm++)
            {
                var buf = new PixelBuffer();
                CapContextRenderer.Apply(buf, flags, palette, matParams, wallParams, xm, ym, seed);
                tiles.Add(buf);
            }
        }

        return new TileGrid(tiles, VariantCount, cycle, cycle);
    }

    // variant bits: bit0=N, bit1=E, bit2=S, bit3=W; a set bit means that neighbour is a
    // wall (EdgeFlags semantics: set = wall continues, so no fascia on that side).
    private static EdgeFlags MaskToFlags(int m)
    {
        EdgeFlags f = EdgeFlags.None;
        if ((m & 1) != 0) f |= EdgeFlags.N;
        if ((m & 2) != 0) f |= EdgeFlags.E;
        if ((m & 4) != 0) f |= EdgeFlags.S;
        if ((m & 8) != 0) f |= EdgeFlags.W;
        return f;
    }
}
