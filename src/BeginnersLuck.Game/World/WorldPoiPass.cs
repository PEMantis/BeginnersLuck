using System;
using System.Collections.Generic;
using BeginnersLuck.WorldGen.Data;

namespace BeginnersLuck.Game.World;

public static class WorldPoiPass
{
    public static void Apply(
        int width,
        int height,
        byte[] terrainFlat,
        ushort[] flagsFlat,
        Random rng)
    {
        if (terrainFlat.Length != width * height) throw new ArgumentException("terrainFlat size mismatch");
        if (flagsFlat.Length != width * height) throw new ArgumentException("flagsFlat size mismatch");

        PlaceRuins(width, height, terrainFlat, flagsFlat, rng);
        // Roads should come after towns exist (flags already have Town bits)
        PlaceRoads(width, height, terrainFlat, flagsFlat, rng);
    }

    private static void PlaceRuins(int w, int h, byte[] terrain, ushort[] flags, Random rng)
    {
        // Sparse clusters. Tune later.
        int targetClusters = Math.Clamp((w * h) / 2200, 8, 60);

        for (int c = 0; c < targetClusters; c++)
        {
            // Pick a random inland-ish seed point
            int sx = rng.Next(0, w);
            int sy = rng.Next(0, h);

            if (!IsLand((TileId)terrain[sx + sy * w]))
                continue;

            // Don't place on towns
            if ((((TileFlags)flags[sx + sy * w]) & TileFlags.Town) != 0)
                continue;

            // Cluster radius + density
            int radius = rng.Next(2, 6);
            int drops = rng.Next(6, 18);

            for (int k = 0; k < drops; k++)
            {
                int x = Math.Clamp(sx + rng.Next(-radius, radius + 1), 0, w - 1);
                int y = Math.Clamp(sy + rng.Next(-radius, radius + 1), 0, h - 1);

                int i = x + y * w;
                var tid = (TileId)terrain[i];
                if (!IsLand(tid)) continue;

                // Avoid cliffs/coasts if you want ruins to feel "explorable"
                var f = (TileFlags)flags[i];
                if ((f & (TileFlags.Coast | TileFlags.Cliff)) != 0)
                    continue;

                // Mark ruin POI
                flags[i] = (ushort)(f | TileFlags.Ruins);
            }
        }
    }

    private static void PlaceRoads(int w, int h, byte[] terrain, ushort[] flags, Random rng)
    {
        // Collect towns
        var towns = new List<int>();

        for (int i = 0; i < flags.Length; i++)
        {
            var f = (TileFlags)flags[i];
            if ((f & TileFlags.Town) != 0)
                towns.Add(i);
        }

        if (towns.Count < 2)
            return;

        // Connect each town to its nearest neighbor (simple MST-ish)
        // This makes a light network without going full pathfinding complexity.
        for (int a = 0; a < towns.Count; a++)
        {
            int ia = towns[a];
            int ax = ia % w;
            int ay = ia / w;

            int best = -1;
            int bestD = int.MaxValue;

            for (int b = 0; b < towns.Count; b++)
            {
                if (a == b) continue;
                int ib = towns[b];
                int bx = ib % w;
                int by = ib / w;

                int d = Math.Abs(ax - bx) + Math.Abs(ay - by);
                if (d < bestD)
                {
                    bestD = d;
                    best = ib;
                }
            }

            if (best >= 0)
                CarveRoadManhattan(w, h, terrain, flags, ia, best);
        }
    }

    private static void CarveRoadManhattan(int w, int h, byte[] terrain, ushort[] flags, int start, int goal)
    {
        int x = start % w;
        int y = start / w;

        int gx = goal % w;
        int gy = goal / w;

        // Random-ish tie-breaking gives variety but stays deterministic if rng seeded earlier.
        // Here we keep it deterministic without extra rng: alternate axis each step.
        bool xFirst = true;

        int safety = w * h;
        while ((x != gx || y != gy) && safety-- > 0)
        {
            int i = x + y * w;
            var tid = (TileId)terrain[i];

            // Only place roads on land tiles (skip water)
            if (IsLand(tid))
            {
                var f = (TileFlags)flags[i];
                flags[i] = (ushort)(f | TileFlags.Road);
            }

            int dx = gx - x;
            int dy = gy - y;

            if (xFirst)
            {
                if (dx != 0) x += Math.Sign(dx);
                else if (dy != 0) y += Math.Sign(dy);
            }
            else
            {
                if (dy != 0) y += Math.Sign(dy);
                else if (dx != 0) x += Math.Sign(dx);
            }

            xFirst = !xFirst;

            if ((uint)x >= (uint)w || (uint)y >= (uint)h)
                break;
        }
    }

    private static bool IsLand(TileId tid)
        => tid is not (TileId.DeepWater or TileId.Ocean or TileId.ShallowWater);
}
