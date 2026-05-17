using TileGeneratorV2.Core.Materials;
using TileGeneratorV2.Core.Structure;
using TileGeneratorV2.Core.Util;

namespace TileGeneratorV2.Core.Context;

/// <summary>
/// Renders one cap tile (wall top-view). Black base; fascia strips at exposed edges.
///
/// Strip width is fw+1 pixels. FasciaWidth() returns a whole number of brick courses
/// (fw = k*H) when the brick fits the depth budget, so wy=fw lands on a real mortar
/// joint — the inner edge is rendered identically to the joints between courses.
/// Tall bricks (H > budget) get a single narrower cut course; its inner edge has no
/// real joint, so Apply() substitutes the brick's own bed joint there for a 2px groove.
///
/// Coordinate mapping (wy = wall-depth; 0 = outer edge mortar, fw = inner edge):
///   N strip:  wx = tileOx+px,  wy = py
///   S strip:  wx = tileOx+px,  wy = 31-py
///   W strip:  wx = tileOy+py,  wy = px       (perpendicular)
///   E strip:  wx = tileOy+py,  wy = 31-px    (perpendicular)
///
/// Corner grooves split competing open edges with a 45° diagonal rendered as mortar
/// (wy=0 forces mortar coloring), giving four trapezoids on a fully-surrounded cap.
/// Grooves are 3 px wide (±1 from the centre line) to match the visual weight of mortar joints.
///
///   NW groove: |py-px| &lt;= 1        (N owns py &lt; px-1, W owns py &gt; px+1)
///   SE groove: |py-px| &lt;= 1        (S owns py &gt; px+1, E owns py &lt; px-1)
///   NE groove: |px+py-31| &lt;= 1     (N owns px+py &lt; 30, E owns px+py &gt; 32)
///   SW groove: |px+py-31| &lt;= 1     (S owns px+py &gt; 32, W owns px+py &lt; 30)
///
/// EdgeFlags: 1 = wall continues, 0 = open space. Fascia appears on open sides.
/// </summary>
public static class CapContextRenderer
{
    private const int Size = PixelBuffer.Size;
    private const int Last = Size - 1;   // 31

    public static void Apply(
        PixelBuffer buffer,
        EdgeFlags flags,
        ColorPalette palette,
        MaterialParameters? matParams,
        StructureParameters wallParams,
        int worldTileX,
        int worldTileY,
        int seed)
    {
        int fw  = FasciaWidth(wallParams);
        int fw1 = fw + 1;   // +1 to include the inner mortar line at wy=fw
        int  brickH    = wallParams.BrickHeight;
        bool cutCourse = fw % brickH != 0;   // brick taller than budget — inner edge falls mid-brick

        buffer.Fill(ColorRgba.Black);

        var mat = new MaterialGenerator().Generate(seed, palette, matParams);

        bool openN = (flags & EdgeFlags.N) == 0;
        bool openE = (flags & EdgeFlags.E) == 0;
        bool openS = (flags & EdgeFlags.S) == 0;
        bool openW = (flags & EdgeFlags.W) == 0;

        int tileOx = worldTileX * Size;
        int tileOy = worldTileY * Size;

        for (int py = 0; py < Size; py++)
        for (int px = 0; px < Size; px++)
        {
            bool inN = py < fw1;
            bool inS = py >= Size - fw1;
            bool inW = px < fw1;
            bool inE = px >= Size - fw1;

            bool paint = false, groove = false;
            int  wx = 0, wy = 0;
            int  mx = px, my = py;   // material indices; defaults cover N and W strips

            if (inN && inW) // ── NW: groove at |py-px| <= 1 ───────────────────────
            {
                if (openN && openW)
                {
                    if      (py < px - 1) { paint = true; wx = tileOx + px;  wy = py;  }
                    else if (py > px + 1) { paint = true; wx = tileOy + py;  wy = px;  }
                    else                  { groove = true; }
                }
                else if (openN) { paint = true; wx = tileOx + px;  wy = py;  }
                else if (openW) { paint = true; wx = tileOy + py;  wy = px;  }
            }
            else if (inN && inE) // ── NE: groove at |px+py-Last| <= 1 ─────────────
            {
                if (openN && openE)
                {
                    if      (px + py < Last - 1) { paint = true; wx = tileOx + px;  wy = py;        my = py;        }
                    else if (px + py > Last + 1) { paint = true; wx = tileOy + py;  wy = Last - px; mx = Last - px; }
                    else                         { groove = true; }
                }
                else if (openN) { paint = true; wx = tileOx + px;  wy = py;        my = py;        }
                else if (openE) { paint = true; wx = tileOy + py;  wy = Last - px; mx = Last - px; }
            }
            else if (inS && inW) // ── SW: groove at |px+py-Last| <= 1 ─────────────
            {
                if (openS && openW)
                {
                    if      (px + py > Last + 1) { paint = true; wx = tileOx + px;  wy = Last - py;  my = Last - py;  }
                    else if (px + py < Last - 1) { paint = true; wx = tileOy + py;  wy = px;                          }
                    else                         { groove = true; }
                }
                else if (openS) { paint = true; wx = tileOx + px;  wy = Last - py;  my = Last - py;  }
                else if (openW) { paint = true; wx = tileOy + py;  wy = px;                           }
            }
            else if (inS && inE) // ── SE: groove at |py-px| <= 1 ──────────────────
            {
                if (openS && openE)
                {
                    if      (py > px + 1) { paint = true; wx = tileOx + px;  wy = Last - py;  my = Last - py;  }
                    else if (py < px - 1) { paint = true; wx = tileOy + py;  wy = Last - px;  mx = Last - px;  }
                    else                  { groove = true; }
                }
                else if (openS) { paint = true; wx = tileOx + px;  wy = Last - py;  my = Last - py;  }
                else if (openE) { paint = true; wx = tileOy + py;  wy = Last - px;  mx = Last - px;  }
            }
            // ── Pure (non-corner) strips ──────────────────────────────────────────
            else if (inN) { paint = openN; wx = tileOx + px;  wy = py;        my = py;        }
            else if (inS) { paint = openS; wx = tileOx + px;  wy = Last - py; my = Last - py; }
            else if (inW) { paint = openW; wx = tileOy + py;  wy = px;                        }
            else if (inE) { paint = openE; wx = tileOy + py;  wy = Last - px; mx = Last - px; }

            if (groove)
                // wy=0 guarantees mortar coloring (outer bed joint depth) for the groove line
                buffer[px, py] = StructureGenerator.ApplyCapFasciaPixel(mat[px, py], tileOx + px, 0, wallParams);
            else if (paint)
            {
                // A cut course has no real mortar joint at its inner edge — substitute the
                // brick's own bed joint so the fascia ends with a groove identical to the
                // joints between courses (2px: localY = H-1 then 0).
                int swy = wy;
                if (cutCourse && wy == fw)     swy = brickH;
                if (cutCourse && wy == fw - 1) swy = brickH - 1;
                buffer[px, py] = StructureGenerator.ApplyCapFasciaPixel(mat[mx, my], wx, swy, wallParams);
            }
        }
    }

    /// <summary>
    /// Fascia depth in pixels. Returns a whole number of brick courses (fw = k*H, k = 1–2)
    /// when the brick fits the Size/3 depth budget, so the inner edge (wy=fw) lands on a
    /// real mortar joint. Bricks taller than the budget yield a single narrower cut course.
    /// </summary>
    public static int FasciaWidth(StructureParameters wallParams)
    {
        int h   = wallParams.BrickHeight;
        int max = Size / 3;                  // depth budget
        if (h > max) return max;             // brick taller than budget — one narrower cut course
        return (max / h) * h;                // whole courses (k*H) that fit the budget
    }
}
