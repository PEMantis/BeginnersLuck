using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace BeginnersLuck.Engine.World;

public sealed class TileMap
{
    public int Width { get; }
    public int Height { get; }
    public int TileSize { get; }

    // tile indices/ids, row-major: index = x + y*Width
    public int[] Tiles { get; }

    // Optional: collision flags per tile id (tileId -> solid?)
    private readonly HashSet<int> _solidTileIds = new();

    // Per-cell solidity (what you want for local maps / walls / rivers / etc)
    // -1 = no override, 0 = explicitly not solid, 1 = solid
    private readonly sbyte[] _solidOverride;


    public TileMap(int width, int height, int tileSize, int[] tiles)
    {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (tileSize <= 0) throw new ArgumentOutOfRangeException(nameof(tileSize));
        if (tiles == null) throw new ArgumentNullException(nameof(tiles));
        if (tiles.Length != width * height)
            throw new ArgumentException($"Tiles length {tiles.Length} != width*height {width * height}");

        Width = width;
        Height = height;
        TileSize = tileSize;
        Tiles = tiles;

        _solidOverride = new sbyte[Width * Height];
        Array.Fill(_solidOverride, (sbyte)-1);

    }

    public Point WorldToCell(Vector2 worldPos)
        => new((int)MathF.Floor(worldPos.X / TileSize), (int)MathF.Floor(worldPos.Y / TileSize));

    public Vector2 CellToWorldTopLeft(Point cell)
        => new(cell.X * TileSize, cell.Y * TileSize);

    public Vector2 CellToWorldCenter(Point cell)
        => new(cell.X * TileSize + TileSize * 0.5f, cell.Y * TileSize + TileSize * 0.5f);

    public int Index(int x, int y) => x + y * Width;

    // TileMapRenderer expects GetTile(x,y)
    public int GetTile(int x, int y)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height) return -1;
        return Tiles[Index(x, y)];
    }

    // Alias that reads nicer in game code
    public int GetTileId(int x, int y) => GetTile(x, y);

    public void SetTile(int x, int y, int tileId)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height) return;
        Tiles[Index(x, y)] = tileId;
    }

    // Per-tile-id solidity (optional)
    public void SetSolid(int tileId, bool solid)
    {
        if (solid) _solidTileIds.Add(tileId);
        else _solidTileIds.Remove(tileId);
    }

    public bool IsSolidTileId(int tileId) => _solidTileIds.Contains(tileId);

    // Per-cell solidity (what LocalMapScene should use)
    public void SetSolidCell(int x, int y, bool solid)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height) return;
        _solidOverride[Index(x, y)] = (sbyte)(solid ? 1 : 0);
    }

    public bool IsSolidCell(int x, int y)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height) return true;

        int i = Index(x, y);

        // Override wins (both solid and NOT solid)
        sbyte ov = _solidOverride[i];
        if (ov != -1) return ov == 1;

        // Otherwise fall back to tile-id rules
        int id = Tiles[i];
        return id >= 0 && IsSolidTileId(id);
    }

}
