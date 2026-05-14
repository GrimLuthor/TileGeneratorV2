namespace TileGeneratorV2.Core.Materials;

public enum MaterialType
{
    Auto,
    Cobblestone,  // Voronoi F1 + per-cell variation — distinct cells with internal shading
    Marble,       // Perlin + sin turbulence — veined, flowing organic patterns
    Granite,      // High-freq multi-octave Perlin — speckled, granular
    SmoothStone,  // Low-freq Perlin, gentle — subtle variation, near-flat
    Rough,        // Domain-warped Perlin — organic, slightly chaotic
    Layered,      // Anisotropic fBm — horizontal sedimentary/sandstone bands
    Cracked,      // Voronoi F2–F1 — network of crack/grout lines with stone interiors
    Cellular,     // Per-cell random color — irregular mosaic / patchwork
    Wavy,         // Nested sine interference — organic ripple / channel patterns
    Ridged,       // Ridged multifractal — sharp bright ridges, rock strata look
    Wood,         // Concentric rings from grid centres + turbulence warp
    Contour       // sin(fBm) — topographic contour lines, flowing organic curves
}
