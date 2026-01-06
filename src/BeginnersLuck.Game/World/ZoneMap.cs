
using System;
using Microsoft.Xna.Framework;

namespace BeginnersLuck.Game.World;

public sealed class ZoneMap
{
    private readonly ZoneId[] _zones;

    public int Width { get; }
    public int Height { get; }

    public ZoneMap(int width, int height, ZoneId[] zones)
    {
        Width = width;
        Height = height;
        _zones = zones;
    }

    public ZoneId GetZone(Point cell) => GetZone(cell.X, cell.Y);

    public ZoneId GetZone(int x, int y)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height)
            return ZoneId.None;

        return _zones[y * Width + x];
    }

    public ZoneInfo GetInfo(int x, int y)
    {
        var id = GetZone(x, y);

        // Simple defaults for now (we’ll evolve these into proper tuning later)
        return id switch
        {
            ZoneId.Road => new ZoneInfo(id, danger: 1, encounterTableId: "road"),
            ZoneId.Grasslands => new ZoneInfo(id, danger: 2, encounterTableId: "plains_low"),
            ZoneId.Forest => new ZoneInfo(id, danger: 3, encounterTableId: "plains_high"),
            ZoneId.Ruins => new ZoneInfo(id, danger: 4, encounterTableId: "plains_high"),
            ZoneId.Lake => new ZoneInfo(id, danger: 0, encounterTableId: "none"),
            ZoneId.Mountains => new ZoneInfo(id, danger: 4, encounterTableId: "mountain"),
            _ => new ZoneInfo(ZoneId.None, danger: 0, encounterTableId: "none"),
        };
    }

    public static ZoneMap GenerateFromTiles(
       int width,
       int height,
       Func<int, int, int> getTileId,
       int seed,
       int zoneSizeCells)
    {
        var rng = new Random(seed);
        var zones = new ZoneId[width * height];

        int blocksX = (width + zoneSizeCells - 1) / zoneSizeCells;
        int blocksY = (height + zoneSizeCells - 1) / zoneSizeCells;

        // Decide zone per block
        var blockZones = new ZoneId[blocksX * blocksY];

        for (int by = 0; by < blocksY; by++)
            for (int bx = 0; bx < blocksX; bx++)
            {
                blockZones[by * blocksX + bx] =
                    PickZoneForBlock(bx, by, zoneSizeCells, getTileId, rng);
            }

        // Expand blocks to cells
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int bx = x / zoneSizeCells;
                int by = y / zoneSizeCells;
                zones[y * width + x] = blockZones[by * blocksX + bx];
            }

        return new ZoneMap(width, height, zones);
    }

    private static ZoneId PickZoneForBlock(
        int blockX,
        int blockY,
        int blockSize,
        Func<int, int, int> getTileId,
        Random rng)
    {
        // Sample center tile of block (simple, stable)
        int sampleX = blockX * blockSize + blockSize / 2;
        int sampleY = blockY * blockSize + blockSize / 2;

        int tileId = getTileId(sampleX, sampleY);

        // You can tune this mapping later
        return tileId switch
        {
            0 => ZoneId.Grasslands,
            1 => ZoneId.Forest,
            2 => ZoneId.Road,
            3 => ZoneId.Lake,
            4 => ZoneId.Ruins,
            _ => RollFallback(rng),
        };
    }

    private static ZoneId RollFallback(Random rng)
    {
        int r = rng.Next(100);
        if (r < 50) return ZoneId.Grasslands;
        if (r < 70) return ZoneId.Forest;
        if (r < 85) return ZoneId.Road;
        if (r < 95) return ZoneId.Ruins;
        return ZoneId.Mountains;
    }
}
