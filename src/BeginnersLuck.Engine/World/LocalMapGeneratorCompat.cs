using System;
using BeginnersLuck.Engine.World;
using BeginnersLuck.WorldGen.Data;
using BeginnersLuck.WorldGen.Local;

namespace BeginnersLuck.WorldGen;

/// <summary>
/// Temporary generator to guarantee playability.
/// Purpose: stop "locked-in mountain bowl" spawns immediately.
/// Replace later with your real generator.
/// </summary>
public static class LocalMapGeneratorCompat
{
    public static LocalMapData Generate(WorldMap world, int seed, int wx, int wy, int size, LocalMapPurpose purpose)
    {
        var rng = new Random(seed ^ (wx * 73856093) ^ (wy * 19349663));

        int count = size * size;

        var elevation = new byte[count];
        var moisture = new byte[count];
        var temperature = new byte[count];

        var terrain = new TileId[count];
        var flags = new TileFlags[count];

        // Base: mostly grass (big connected component)
        for (int i = 0; i < count; i++)
        {
            elevation[i] = (byte)rng.Next(0, 256);
            moisture[i] = (byte)rng.Next(0, 256);
            temperature[i] = (byte)rng.Next(0, 256);
            terrain[i] = TileId.Grass;
            flags[i] = TileFlags.None;
        }

        // Add a mountain "rim" around the map edge (but keep a wide playable interior)
        int rim = Math.Max(3, size / 24);
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            if (x < rim || y < rim || x >= size - rim || y >= size - rim)
                terrain[x + y * size] = TileId.Mountain;
        }

        // Punch 2–4 exits through the rim to guarantee escape routes
        int exits = 3;
        for (int e = 0; e < exits; e++)
        {
            int side = rng.Next(0, 4);
            int pos = rng.Next(rim + 6, size - rim - 6);

            for (int w = -2; w <= 2; w++)
            {
                int x = 0, y = 0;
                if (side == 0) { x = rim; y = pos + w; }           // W
                if (side == 1) { x = size - rim - 1; y = pos + w; } // E
                if (side == 2) { x = pos + w; y = rim; }           // N
                if (side == 3) { x = pos + w; y = size - rim - 1; } // S

                if ((uint)x < (uint)size && (uint)y < (uint)size)
                    terrain[x + y * size] = TileId.Grass;
            }
        }

        // Add some water blobs away from center (never fully enclosing the interior)
        int blobs = Math.Max(3, size / 32);
        for (int b = 0; b < blobs; b++)
        {
            int cx = rng.Next(rim + 10, size - rim - 10);
            int cy = rng.Next(rim + 10, size - rim - 10);
            int r = rng.Next(3, 8);

            for (int y = cy - r; y <= cy + r; y++)
            for (int x = cx - r; x <= cx + r; x++)
            {
                if ((uint)x >= (uint)size || (uint)y >= (uint)size) continue;
                int dx = x - cx, dy = y - cy;
                if (dx * dx + dy * dy <= r * r)
                    terrain[x + y * size] = (rng.NextDouble() < 0.7) ? TileId.ShallowWater : TileId.DeepWater;
            }
        }

        // Roads (simple cross + purpose tweaks)
        int mid = size / 2;
        for (int x = rim + 1; x < size - rim - 1; x++)
            flags[x + mid * size] |= TileFlags.Road;

        for (int y = rim + 1; y < size - rim - 1; y++)
            flags[mid + y * size] |= TileFlags.Road;

        // Ensure roads are on walkable terrain
        for (int x = 0; x < size; x++)
        {
            ForceWalkable(terrain, flags, size, x, mid);
        }
        for (int y = 0; y < size; y++)
        {
            ForceWalkable(terrain, flags, size, mid, y);
        }

        // Rivers are overlays only (do NOT block right now)
        // Put a simple meandering river sometimes
        if (rng.NextDouble() < 0.6)
        {
            int y = rng.Next(rim + 8, size - rim - 8);
            for (int x = rim + 1; x < size - rim - 1; x++)
            {
                int yy = y + (int)MathF.Round(MathF.Sin((x + seed) * 0.12f) * 2f);
                if ((uint)yy < (uint)size)
                    flags[x + yy * size] |= TileFlags.River;
            }
        }

        // Town
        (int X, int Y)? townCenter = null;
        if (purpose == LocalMapPurpose.Town)
        {
            townCenter = (mid, mid);
            flags[mid + mid * size] |= TileFlags.Town;
            terrain[mid + mid * size] = TileId.Dirt;

            // Flatten a little plaza
            for (int y = mid - 4; y <= mid + 4; y++)
            for (int x = mid - 4; x <= mid + 4; x++)
            {
                if ((uint)x >= (uint)size || (uint)y >= (uint)size) continue;
                terrain[x + y * size] = TileId.Dirt;
            }
        }

        // Portals: put road portals on all edges where the cross-road hits
        var portals = new EdgePortals
        {
            RoadN = true, RoadE = true, RoadS = true, RoadW = true,
            RoadNPos = (ushort)mid, RoadSPos = (ushort)mid,
            RoadWPos = (ushort)mid, RoadEPos = (ushort)mid,

            RiverN = false, RiverE = false, RiverS = false, RiverW = false,
            RiverNPos = 0, RiverEPos = 0, RiverSPos = 0, RiverWPos = 0
        };

        return new LocalMapData(
            size, seed, wx, wy,
            purpose, (BiomeId)0,
            elevation, moisture, temperature,
            terrain, flags,
            portals, townCenter);
    }

    private static void ForceWalkable(TileId[] terrain, TileFlags[] flags, int n, int x, int y)
    {
        int i = x + y * n;

        // If road goes over blocked terrain, downgrade it to dirt/grass.
        if (terrain[i] is TileId.DeepWater or TileId.ShallowWater or TileId.Ocean or TileId.Coast or TileId.Mountain)
            terrain[i] = TileId.Dirt;

        // Clear cliff on roads
        flags[i] &= ~TileFlags.Cliff;
    }
}
