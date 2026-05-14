using TileGeneratorV2.Core.Util;

namespace TileGeneratorV2.Core.Noise;

/// <summary>
/// Voronoi F2–F1: produces 0 near cell boundaries (cracks/grout lines) and
/// 1 in cell interiors. Maps naturally to Primary=stone, Secondary=crack.
/// </summary>
public class CrackedNoise : INoiseSampler
{
    private readonly int _seed;
    // How wide the cracks are relative to cell size (smaller = hairline cracks)
    private readonly float _crackWidth;

    public CrackedNoise(int seed, float crackWidth = 0.18f)
    {
        _seed = seed;
        _crackWidth = crackWidth;
    }

    public float SampleTileable(float x, float y, int tileSize, float frequency = 4f)
    {
        int numCells = Math.Max(1, (int)MathF.Round(frequency));
        float cellSize = tileSize / (float)numCells;
        float fx = x / cellSize;
        float fy = y / cellSize;
        int cellX = (int)MathF.Floor(fx);
        int cellY = (int)MathF.Floor(fy);

        float minDist1 = float.MaxValue;
        float minDist2 = float.MaxValue;

        for (int dy = -1; dy <= 1; dy++)
        for (int dx = -1; dx <= 1; dx++)
        {
            int wx = ((cellX + dx) % numCells + numCells) % numCells;
            int wy = ((cellY + dy) % numCells + numCells) % numCells;
            var (jx, jy) = SeededRandom.Hash2(_seed, wx + wy * 397, wy + wx * 23);
            float pointX = (cellX + dx) + jx;
            float pointY = (cellY + dy) + jy;
            float dist = MathF.Sqrt((fx - pointX) * (fx - pointX) + (fy - pointY) * (fy - pointY));

            if (dist < minDist1) { minDist2 = minDist1; minDist1 = dist; }
            else if (dist < minDist2) { minDist2 = dist; }
        }

        // F2–F1: 0 at cell boundary, positive inside cells
        float border = minDist2 - minDist1;
        return Math.Clamp(border / _crackWidth, 0f, 1f);
    }
}
