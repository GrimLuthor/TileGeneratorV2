using TileGeneratorV2.Core.Util;

namespace TileGeneratorV2.Core.Noise;

/// <summary>
/// Assigns each Voronoi cell a random fixed value from the seed.
/// Produces a mosaic / patchwork of different-colored irregular regions — no
/// internal gradient, just solid cells of varying color.
/// </summary>
public class CellularNoise : INoiseSampler
{
    private readonly int _seed;

    public CellularNoise(int seed) => _seed = seed;

    public float SampleTileable(float x, float y, int tileSize, float frequency = 4f)
    {
        int numCells = Math.Max(1, (int)MathF.Round(frequency));
        float cellSize = tileSize / (float)numCells;
        float fx = x / cellSize;
        float fy = y / cellSize;
        int cellX = (int)MathF.Floor(fx);
        int cellY = (int)MathF.Floor(fy);

        float minDist = float.MaxValue;
        int closestWX = 0, closestWY = 0;

        for (int dy = -1; dy <= 1; dy++)
        for (int dx = -1; dx <= 1; dx++)
        {
            int wx = ((cellX + dx) % numCells + numCells) % numCells;
            int wy = ((cellY + dy) % numCells + numCells) % numCells;
            var (jx, jy) = SeededRandom.Hash2(_seed, wx + wy * 397, wy + wx * 23);
            float pointX = (cellX + dx) + jx;
            float pointY = (cellY + dy) + jy;
            float dist = MathF.Sqrt((fx - pointX) * (fx - pointX) + (fy - pointY) * (fy - pointY));

            if (dist < minDist) { minDist = dist; closestWX = wx; closestWY = wy; }
        }

        // Each cell gets a deterministic random value based on its wrapped position
        return SeededRandom.Hash1(_seed ^ unchecked((int)0xBEEFCAFE), closestWX * 131 + closestWY * 1009);
    }
}
