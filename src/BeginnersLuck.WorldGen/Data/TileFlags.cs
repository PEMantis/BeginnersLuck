namespace BeginnersLuck.WorldGen.Data;

[Flags]
public enum TileFlags : ushort
{
    None = 0,
    River = 1 << 0,
    Road  = 1 << 1,
    Coast = 1 << 2,
    Cliff = 1 << 3,
    Forest = 1 << 4,
    // extend later
}
