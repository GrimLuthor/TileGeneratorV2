# TileGeneratorV2 — Exported Tileset Format

A tileset export is a folder containing `manifest.json` plus one PNG atlas per tile
class. This document explains how a game / consumer reads it.

## Files

| File            | Contents                                       |
|-----------------|------------------------------------------------|
| `manifest.json` | describes each tile class and how to index it  |
| `cap.png`       | wall-cap tiles — a blob autotile               |
| `wall.png`      | wall-surface tiles — solid fill                |

## manifest.json

```json
{
  "seed": 1234567,
  "tileSize": 32,
  "indexFormula": "idx = variant*(cycleX*cycleY) + (worldY%cycleY)*cycleX + (worldX%cycleX); atlasCol = idx % atlasColumns; atlasRow = idx / atlasColumns",
  "cap":  { "atlas": "cap.png",  "variants": 16, "cycleX": 3, "cycleY": 3, "atlasColumns": 9 },
  "wall": { "atlas": "wall.png", "variants": 1,  "cycleX": 3, "cycleY": 1, "atlasColumns": 3 }
}
```

Per-class fields:

- `atlas` — the PNG filename.
- `variants` — `1` = plain surface fill; `>1` = autotile, where the variant is chosen
  from the cell's neighbours (see below).
- `cycleX` / `cycleY` — the world-position cycle. The pattern repeats every `cycleX`
  tiles horizontally and `cycleY` tiles vertically. Always `1` or `3`.
- `atlasColumns` — atlas width in tiles. The atlas is a row-major grid.

A class is present in the manifest only if it was exported.

## Choosing a tile

For a map cell at world tile coordinate `(worldX, worldY)`:

### 1. Variant

- **Surface class** (`variants == 1`): `variant = 0`.
- **Cap** (autotile, `variants == 16`): `variant = N + 2·E + 4·S + 8·W`,
  where `N/E/S/W` is `1` if the neighbouring cell in that direction is a wall, else `0`.
  - `variant 0`  — free-standing cap: brick fascia on all four sides.
  - `variant 15` — fully enclosed: no fascia, solid interior.

### 2. Linear index

```
xm  = ((worldX % cycleX) + cycleX) % cycleX     // floored mod → 0..cycleX-1
ym  = ((worldY % cycleY) + cycleY) % cycleY
idx = variant * (cycleX * cycleY) + ym * cycleX + xm
```

Use a floored modulo so negative world coordinates work.

### 3. Atlas cell

```
atlasCol = idx % atlasColumns
atlasRow = idx / atlasColumns                   // integer division
srcRect  = (atlasCol*tileSize, atlasRow*tileSize, tileSize, tileSize)
```

## Worked example

`wall` with `cycleX=3, cycleY=1, atlasColumns=3`, cell at `(worldX=7, worldY=4)`:

```
variant  = 0                       (surface class)
xm = 7 % 3 = 1,  ym = 4 % 1 = 0
idx      = 0*(3*1) + 0*3 + 1 = 1
atlasCol = 1 % 3 = 1,  atlasRow = 1 / 3 = 0
→ tile at pixel (32, 0) in wall.png
```

## Notes

- Every tile is `tileSize × tileSize` (32×32). Atlases are 32-bit RGBA PNG.
- `cycle 1` means the class self-tiles on that axis — one tile fills any region.
- `wall.png` is the wall *surface* only (solid-fill interior). Edge-aware wall-face
  tiles are not exported yet.
- Floor tiles are not exported yet.
