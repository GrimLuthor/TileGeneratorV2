using TileGeneratorV2.Core.Util;

namespace TileGeneratorV2.Core.Noise;

/// <summary>
/// Voronoi F1 noise blended with per-cell random color.
/// Pure F1 produces circular gradient blobs; mixing in a cell-ID hash
/// gives each cell a distinct base value so adjacent cells look different.
/// </summary>
public class VoronoiNoise : INoiseSampler
{
    private readonly int _seed;
    // 0.0 = pure F1 distance gradient (blob), 1.0 = pure per-cell mosaic
    private readonly float _cellBlend;

    public VoronoiNoise(int seed, float cellBlend = 0.65f)
    {
        _seed = seed;
        _cellBlend = cellBlend;
    }

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

        // F1 normalized: 0 at cell center, 1 at typical cell boundary
        float f1 = Math.Clamp(minDist / 0.65f, 0f, 1f);
        // Per-cell hash: gives each cell its own base brightness
        float cellHash = SeededRandom.Hash1(_seed ^ unchecked((int)0xBEEFCAFE), closestWX * 131 + closestWY * 1009);

        // Blend: cell hash dominates, F1 adds internal shading
        return f1 * (1f - _cellBlend) + cellHash * _cellBlend;
    }
}
