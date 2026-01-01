using Microsoft.Xna.Framework;
namespace BeginnersLuck.Engine.World;

public sealed class TileMap
{
    public int Width { get; }
    public int Height { get; }
    public int TileSize { get; }

    // tile ids, row-major: index = x + y*Width
    public int[] Tiles { get; }

    // optional: collision flags per tile id (tileId -> solid?)
    private readonly HashSet<int> _solidTileIds = new();

    public Point WorldToCell(Vector2 worldPos)
        => new((int)MathF.Floor(worldPos.X / TileSize), (int)MathF.Floor(worldPos.Y / TileSize));

    public Vector2 CellToWorldTopLeft(Point cell)
        => new(cell.X * TileSize, cell.Y * TileSize);

    public Vector2 CellToWorldCenter(Point cell)
        => new(cell.X * TileSize + TileSize * 0.5f, cell.Y * TileSize + TileSize * 0.5f);

    public TileMap(int width, int height, int tileSize, int[] tiles)
    {
        Width = width;
        Height = height;
        TileSize = tileSize;
        Tiles = tiles;
    }

    public int GetTile(int x, int y)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height) return -1;
        return Tiles[x + y * Width];
    }

    public int GetTileId(int x, int y)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height) return -1;
        return Tiles[y * Width + x]; // use your actual backing array name
    }

    public void SetSolid(int tileId, bool solid)
    {
        if (solid) _solidTileIds.Add(tileId);
        else _solidTileIds.Remove(tileId);
    }

    public bool IsSolidTileId(int tileId) => _solidTileIds.Contains(tileId);

    public bool IsSolidCell(int x, int y)
    {
        int id = GetTile(x, y);
        return id >= 0 && IsSolidTileId(id);
    }
}
