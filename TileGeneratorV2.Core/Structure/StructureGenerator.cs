using TileGeneratorV2.Core.Util;

namespace TileGeneratorV2.Core.Structure;

/// <summary>
/// Stage 2: applies a brick/stone bond pattern to an existing material buffer.
/// Operates in world coordinates so patterns continue seamlessly across tile boundaries.
/// worldTileX/Y are tile-grid positions (in tile units, not pixels).
/// </summary>
public class StructureGenerator
{
    public void Apply(PixelBuffer buffer, int worldTileX, int worldTileY, StructureParameters p)
    {
        int size    = PixelBuffer.Size;
        int offsetX = worldTileX * size;
        int offsetY = worldTileY * size;

        for (int py = 0; py < size; py++)
        for (int px = 0; px < size; px++)
        {
            int wx = offsetX + px;
            int wy = offsetY + py;
            var m = SampleMortar(wx, wy, p);
            if (!m.IsStone)
                buffer[px, py] = ApplyGroove(buffer[px, py], m, wx, wy, p.Seed);
        }
    }

    // ── Groove rendering ──────────────────────────────────────────────────────

    private static ColorRgba ApplyGroove(ColorRgba pixel, MortarSample m, int wx, int wy, int seed)
    {
        // Per-brick variation: ±20% depth multiplier so each brick's groove reads slightly different
        float unitVar = (SeededRandom.Hash1(seed ^ unchecked(m.UnitId * 1234567), m.UnitId) - 0.5f) * 0.40f;

        // Pixel-level micro-imperfection: makes groove edges look hand-cut rather than perfect
        float pixelVar = (SeededRandom.Hash1(unchecked(wx * 1619 + wy * 31337), seed) - 0.5f) * 0.16f;

        // Shadow gradient: top edge of horizontal joints is darkest (cast shadow from brick above),
        // bottom edge gets a little uplighting. Vertical joints are neutral.
        float t = m.Depth * (1f + unitVar) * (1f + m.Shadow * 0.40f) + pixelVar * m.Depth;
        t = Math.Clamp(t, 0f, 1f);

        // Per-brick darkness multiplier: 0.3–1.0 so some bricks have grooves as dark as now,
        // others go considerably darker (deep crevices vs shallow scratches).
        float darkVar = 0.30f + SeededRandom.Hash1(unchecked(seed ^ m.UnitId * 7654321), m.UnitId + 1) * 0.70f;

        // Derive groove colour from the current pixel's hue, shifted to a much darker lightness.
        // If the pixel is already dark, groove goes slightly lighter (remains visible as a crease).
        ColorMath.RgbToHsl(pixel, out float h, out float s, out float l);
        float grooveLit = l > 0.30f
            ? Math.Max(l * 0.20f * darkVar, 0.02f)   // light material → very dark groove
            : Math.Min(l + 0.10f, 0.35f);             // dark material → slightly lighter groove
        var grooveColor = ColorMath.HslToRgb(h, Math.Max(s - 0.15f, 0f), grooveLit);

        return ColorMath.Lerp(pixel, grooveColor, t);
    }

    // ── Bond dispatch ─────────────────────────────────────────────────────────

    private static MortarSample SampleMortar(int wx, int wy, StructureParameters p) => p.Bond switch
    {
        BondType.Running => RunningBond(wx, wy, p),
        BondType.Stack   => StackBond(wx, wy, p),
        BondType.Ashlar  => AshlarBond(wx, wy, p),
        _                => MortarSample.Stone,
    };

    // ── Running bond ──────────────────────────────────────────────────────────

    private static MortarSample RunningBond(int wx, int wy, StructureParameters p)
    {
        int W      = p.BrickWidth, H = p.BrickHeight;
        int row    = FloorDiv(wy, H);
        int localY = FloorMod(wy, H);
        int offset = FloorMod(row, 2) == 0 ? 0 : W / 2;
        int col    = FloorDiv(wx - offset, W);
        int localX = FloorMod(wx - offset, W);
        return MortarValue(localX, localY, W, H, p.MortarWidth * 0.5f, HashInts(row, col));
    }

    // ── Stack bond ────────────────────────────────────────────────────────────

    private static MortarSample StackBond(int wx, int wy, StructureParameters p)
    {
        int W      = p.BrickWidth, H = p.BrickHeight;
        int row    = FloorDiv(wy, H);
        int col    = FloorDiv(wx, W);
        int localX = FloorMod(wx, W);
        int localY = FloorMod(wy, H);
        return MortarValue(localX, localY, W, H, p.MortarWidth * 0.5f, HashInts(row, col));
    }

    // ── Ashlar bond ───────────────────────────────────────────────────────────

    private static MortarSample AshlarBond(int wx, int wy, StructureParameters p)
    {
        int tileSize = PixelBuffer.Size;
        int H        = p.BrickHeight;
        int row      = FloorDiv(wy, H);
        int localY   = FloorMod(wy, H);
        int localXt  = FloorMod(wx, tileSize);

        var widths = GetAshlarWidths(row, p, tileSize);
        int cumX   = 0;
        for (int i = 0; i < widths.Count; i++)
        {
            int w = widths[i];
            if (localXt < cumX + w)
                return MortarValue(localXt - cumX, localY, w, H, p.MortarWidth * 0.5f, HashInts(row, i));
            cumX += w;
        }
        return MortarSample.Stone;
    }

    // ── Shared mortar helper ──────────────────────────────────────────────────

    private static MortarSample MortarValue(int localX, int localY, int W, int H, float mHalf, int unitId)
    {
        if (mHalf <= 0f) return MortarSample.Stone;

        float dX = Math.Min(localX, W - 1 - localX);
        float dY = Math.Min(localY, H - 1 - localY);

        float d, shadow;
        if (dX <= dY)
        {
            d      = dX;
            shadow = 0f;                                       // vertical joint — no directional shadow
        }
        else
        {
            d      = dY;
            shadow = localY < H / 2 ? 1f : -0.5f;             // top = cast shadow, bottom = slight light
        }

        float t     = Math.Clamp(d / mHalf, 0f, 1f);
        float depth = 1f - t * t * (3f - 2f * t);             // smoothstep: 1 at edge → 0 at interior
        return depth <= 0f ? MortarSample.Stone : new MortarSample(depth, shadow, unitId);
    }

    // ── Ashlar width generation ───────────────────────────────────────────────

    private static List<int> GetAshlarWidths(int row, StructureParameters p, int tileSize)
    {
        int rowSeed = unchecked(p.Seed ^ (row * 374761393 + 1013904223));
        var rng     = new SeededRandom(rowSeed);
        int minW    = Math.Max(4, p.BrickWidth / 2);
        int maxW    = Math.Min(p.BrickWidth * 2, tileSize);
        var widths  = new List<int>();
        int rem     = tileSize;

        while (rem > 0)
        {
            if (rem <= minW) { widths.Add(rem); break; }
            int maxThis = Math.Min(maxW, rem - minW);
            if (maxThis < minW) { widths.Add(rem); break; }
            int w = rng.NextInt(minW, maxThis + 1);
            widths.Add(w);
            rem -= w;
        }
        return widths;
    }

    // ── Math / hash helpers ───────────────────────────────────────────────────

    private static int HashInts(int a, int b)
        => unchecked(a * 374761393 + b * 1013904223);

    private static int FloorDiv(int a, int b)
    {
        int q = a / b;
        return (a < 0 && a % b != 0) ? q - 1 : q;
    }

    private static int FloorMod(int a, int b) => ((a % b) + b) % b;

    // ── Nested value type ─────────────────────────────────────────────────────

    private readonly struct MortarSample
    {
        public readonly float Depth;   // 0 = stone, 1 = groove centre
        public readonly float Shadow;  // +1 = cast shadow (top edge), -0.5 = uplighting, 0 = vertical
        public readonly int   UnitId;  // hashed brick identifier for per-brick variation

        public bool IsStone => Depth <= 0f;

        public MortarSample(float depth, float shadow, int unitId)
        {
            Depth = depth; Shadow = shadow; UnitId = unitId;
        }

        public static readonly MortarSample Stone = default;
    }
}
