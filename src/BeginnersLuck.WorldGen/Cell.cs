namespace BeginnersLuck.WorldGen;

public readonly struct Cell
{
    public readonly int X;
    public readonly int Y;

    public Cell(int x, int y) { X = x; Y = y; }

    public override string ToString() => $"({X},{Y})";
}
