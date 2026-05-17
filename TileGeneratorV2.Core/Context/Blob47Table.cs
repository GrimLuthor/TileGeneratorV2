namespace TileGeneratorV2.Core.Context;

/// <summary>
/// Maps any 8-bit EdgeFlags value to one of the 16 canonical blob-autotile indices.
/// Diagonals are always ignored — only cardinal directions matter.
/// </summary>
public static class Blob47Table
{
    // _index[rawFlags] → 0..15
    private static readonly int[] _index = new int[256];

    /// <summary>All 16 canonical EdgeFlags values, in enumeration order (index = position).</summary>
    public static readonly EdgeFlags[] CanonicalFlags;

    static Blob47Table()
    {
        var seen      = new Dictionary<byte, int>();
        var canonical = new List<EdgeFlags>(16);

        for (int raw = 0; raw < 256; raw++)
        {
            byte norm = Normalize((byte)raw);
            if (!seen.TryGetValue(norm, out int idx))
            {
                idx = canonical.Count;
                seen[norm] = idx;
                canonical.Add((EdgeFlags)norm);
            }
            _index[raw] = idx;
        }

        CanonicalFlags = [.. canonical];
        System.Diagnostics.Debug.Assert(CanonicalFlags.Length == 16,
            $"Blob47Table: expected 16 canonical configs, got {CanonicalFlags.Length}");
    }

    public static int       GetIndex(EdgeFlags flags) => _index[(byte)flags];
    public static EdgeFlags Normalize(EdgeFlags flags) => (EdgeFlags)Normalize((byte)flags);

    // Keep only cardinal bits (N=bit0, E=bit2, S=bit4, W=bit6); zero all diagonal bits.
    private static byte Normalize(byte raw) => (byte)(raw & 0b_0101_0101);
}
