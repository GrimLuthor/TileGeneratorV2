namespace TileGeneratorV2.Core.Noise;

/// <summary>
/// Tileable wood-grain rings: a regular grid of cell centers, each radiating
/// concentric sine rings, perturbed by low-frequency Perlin turbulence.
/// Tiles seamlessly because the cell grid repeats exactly with the tile.
/// </summary>
public class WoodNoise : INoiseSampler
{
    private readonly PerlinNoise _perlin;

    public WoodNoise(int seed) { _perlin = new PerlinNoise(seed); }

    public float SampleTileable(float x, float y, int tileSize, float frequency = 4f)
    {
        // Low-frequency turbulence warp — small enough that rings stay readable
        float warpStrength = tileSize * 0.06f;
        float wx = (_perlin.SampleTileable(x,                    y,                    tileSize, 2f) - 0.5f) * warpStrength;
        float wy = (_perlin.SampleTileable(x + tileSize * 0.37f, y + tileSize * 0.57f, tileSize, 2f) - 0.5f) * warpStrength;

        float px = x + wx;
        float py = y + wy;

        // Grid of ring centres — one centre per cell, grid repeats exactly with tile
        int   numCells = Math.Max(1, (int)MathF.Round(frequency * 0.4f));
        float cellSize = tileSize / (float)numCells;

        float cx = (MathF.Floor(px / cellSize) + 0.5f) * cellSize;
        float cy = (MathF.Floor(py / cellSize) + 0.5f) * cellSize;

        // Shortest toroidal distance to cell centre
        float dx = px - cx;
        float dy = py - cy;
        if (dx >  cellSize * 0.5f) dx -= cellSize;
        if (dx < -cellSize * 0.5f) dx += cellSize;
        if (dy >  cellSize * 0.5f) dy -= cellSize;
        if (dy < -cellSize * 0.5f) dy += cellSize;

        float dist = MathF.Sqrt(dx * dx + dy * dy);

        // Concentric rings, density controlled by frequency
        return MathF.Abs(MathF.Sin(dist / cellSize * frequency * MathF.PI));
    }
}
