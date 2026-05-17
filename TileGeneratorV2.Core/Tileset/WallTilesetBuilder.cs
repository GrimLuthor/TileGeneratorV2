using TileGeneratorV2.Core.Materials;
using TileGeneratorV2.Core.Structure;
using TileGeneratorV2.Core.Util;

namespace TileGeneratorV2.Core.Tileset;

/// <summary>
/// Builds the wall-surface tile set for one seed — the solid-fill interior of a wall.
/// One tile per world-position phase of the brick cycle (CycleX = OddPart(brickWidth),
/// CycleY = OddPart(brickHeight)); there is a single variant (no edge/context tiles).
/// </summary>
public static class WallTilesetBuilder
{
    public static TileGrid Build(
        int seed, ColorPalette palette, MaterialParameters? matParams, StructureParameters wallParams)
    {
        int cx = StructureParameters.OddPart(wallParams.BrickWidth);
        int cy = StructureParameters.OddPart(wallParams.BrickHeight);

        var matGen    = new MaterialGenerator();
        var structGen = new StructureGenerator();
        var tiles     = new List<PixelBuffer>(cx * cy);

        for (int ym = 0; ym < cy; ym++)
        for (int xm = 0; xm < cx; xm++)
        {
            var buf = matGen.Generate(seed, palette, matParams);
            structGen.Apply(buf, xm, ym, wallParams);
            tiles.Add(buf);
        }

        return new TileGrid(tiles, variants: 1, cycleX: cx, cycleY: cy);
    }
}
