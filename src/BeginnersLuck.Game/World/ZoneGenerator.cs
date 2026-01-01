using System;
using Microsoft.Xna.Framework;

namespace BeginnersLuck.Game.World;

public static class ZoneGenerator
{
    public static ZoneMap Generate(
        int mapWidth,
        int mapHeight,
        int seed,
        int blockSizeCells = 8)
    {
        var rng = new Random(seed);

        int blocksX = (mapWidth + blockSizeCells - 1) / blockSizeCells;
        int blocksY = (mapHeight + blockSizeCells - 1) / blockSizeCells;

        // 1) Pick a zone per block
        var blockZones = new ZoneId[blocksX * blocksY];

        for (int by = 0; by < blocksY; by++)
            for (int bx = 0; bx < blocksX; bx++)
            {
                blockZones[by * blocksX + bx] = RollZone(rng, bx, by);
            }

        // 2) Expand blocks to per-cell ZoneId
        var zones = new ZoneId[mapWidth * mapHeight];

        for (int y = 0; y < mapHeight; y++)
            for (int x = 0; x < mapWidth; x++)
            {
                int bx = x / blockSizeCells;
                int by = y / blockSizeCells;
                zones[y * mapWidth + x] = blockZones[by * blocksX + bx];
            }

        return new ZoneMap(mapWidth, mapHeight, zones);
    }

    private static ZoneId RollZone(Random rng, int bx, int by)
    {
        // A tiny bit of structure: roads more common near the center-ish.
        // (We’ll replace with nicer noise later.)
        int roll = rng.Next(0, 100);

        // Weighted choices
        if (roll < 50) return ZoneId.Grasslands; // 50%
        if (roll < 70) return ZoneId.Forest;     // 20%
        if (roll < 80) return ZoneId.Road;       // 10%
        if (roll < 90) return ZoneId.Ruins;      // 10%
        if (roll < 95) return ZoneId.Lake;       // 5%
        return ZoneId.Mountains;                 // 5%
    }
}
