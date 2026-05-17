namespace TileGeneratorV2.Core.Context;

[Flags]
public enum EdgeFlags : byte
{
    None = 0,
    N    = 1 << 0,
    NE   = 1 << 1,
    E    = 1 << 2,
    SE   = 1 << 3,
    S    = 1 << 4,
    SW   = 1 << 5,
    W    = 1 << 6,
    NW   = 1 << 7,
    All  = unchecked((byte)0xFF),
}
