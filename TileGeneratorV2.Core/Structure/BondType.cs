namespace TileGeneratorV2.Core.Structure;

public enum BondType
{
    Running,  // offset rows — classic brick, alternating half-brick offset each row
    Stack,    // aligned columns — no row offset, purely rectangular grid
    Ashlar,   // variable-width stones in fixed-height courses, hand-laid look
}
