namespace TileGeneratorV2.Core.Structure;

public enum BondType
{
    // Wall bonds
    Running,     // offset rows — classic brick, alternating half-brick offset each row
    Stack,       // aligned columns — no row offset, purely rectangular grid
    Ashlar,      // variable-width stones in fixed-height courses, hand-laid look

    // Floor bonds
    StoneSlab,   // large squarish units in a running pattern, thin mortar
    Flagstone,   // variable width AND height per unit — irregular hand-laid look
    Cobblestone, // small irregular units, heavy mortar
    BigSlab,     // entire tile is one unit, groove runs around the perimeter inset from edge
    SquareGrid,  // NxN equal squares per tile; square size seed-derived from {8, 16} px
}
