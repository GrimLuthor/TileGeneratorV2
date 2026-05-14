# Tile Generator v2

WPF/C# procedural tile texture generator. Produces ~106 32×32 tiles (47 floor + 47 wall cap + 12 wall face) per seed, edge-aware and autotile-ready for 2D dungeon games.

## Core Rules

1. **No full-tile gradients in Stage 4.** Only modify pixels within a few pixels of tile edges. This was the #1 v1 bug.
2. **All noise must use toroidal sampling** (4D torus map) — never mirroring or clamping.
3. **Structure patterns compute in world coordinates**, not tile-local 0–31. A brick at y=30 must continue at y=0 in the next tile down.
4. **Mortar uses multiplicative blending** — darken the material color, never stamp a fixed overlay color.
5. **All structural parameters (bond type, brick dims, mortar width, weathering) are seed-derived**, not constants.

## 4-Stage Pipeline

1. **MATERIAL** — tileable noise-based surface texture (Voronoi, Perlin, Marble, etc.)
2. **STRUCTURE** — seed-driven bond pattern → unit ID map (int[32,32]) + mortar mask (float[32,32])
3. **VARIATION + WEATHERING** — per-unit color jitter, surface texture, top-lighting, groove dirt, stains, erosion
4. **CONTEXT** — edge/neighbor-aware borders and shadows, touching only pixels near tile edges

## Key Numbers

- Tile size: 32×32 px
- Floor variants: 47 (blob autotile)
- Cap variants: 47 (blob autotile, dropoff-shadow treatment)
- Wall face: 12 (3 rows × 4 horizontal variants)
- Floor allowed unit widths/heights (for self-tileability): 4, 8, 16, 32

## Project Structure

```
TileGeneratorV2.Core/   ← all generation logic
  Noise/, Materials/, Structure/, Variation/, Context/, Tileset/, Pipeline/, Util/
TileGeneratorV2.App/    ← WPF application
```

## Detailed Reference

Full design details are in the memory system. Ask to recall specific topics:
- Pipeline stage details → `project-stages-detail`
- Autotile / edge flags → `project-autotile`
- Continuity implementation → `project-continuity`
- Mortar rendering → `project-mortar`
- Implementation phases → `project-implementation-phases`
- Architectural decisions → `project-technical-decisions`
- Visual reference analysis → `project-visual-reference`
