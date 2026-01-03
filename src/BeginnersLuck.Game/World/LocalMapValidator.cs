using System;
using System.Collections.Generic;
using BeginnersLuck.Engine.World;
using BeginnersLuck.WorldGen.Data;
using BeginnersLuck.WorldGen.Local;

namespace BeginnersLuck.WorldGen;

public static class LocalMapValidator
{
    public readonly record struct Report(
        bool Playable,
        int WalkableCount,
        int LargestRegion,
        bool LargestTouchesEdge,
        int RoadCount,
        string Reason);

    /// <summary>
    /// Walkability rule MUST match what LocalMapScene treats as solid.
    /// Rivers are overlays for now (NOT blocking).
    /// </summary>
    public static bool IsWalkable(LocalMapData m, int x, int y)
    {
        if ((uint)x >= (uint)m.Size || (uint)y >= (uint)m.Size) return false;

        int i = x + y * m.Size;
        var tid = m.Terrain[i];
        var flags = m.Flags[i];

        // Terrain blocks
        if (tid is TileId.DeepWater or TileId.ShallowWater or TileId.Ocean or TileId.Coast or TileId.Mountain)
            return false;

        // Flag blocks
        if ((flags & TileFlags.Cliff) != 0)
            return false;

        // Rivers intentionally NOT blocked yet (overlay).
        return true;
    }

    public static Report Validate(LocalMapData m, LocalMapPurpose purpose)
    {
        int n = m.Size;
        int total = n * n;

        int walkable = 0;
        int roads = 0;

        for (int i = 0; i < total; i++)
        {
            var flags = m.Flags[i];
            if ((flags & TileFlags.Road) != 0) roads++;

            int x = i % n;
            int y = i / n;
            if (IsWalkable(m, x, y)) walkable++;
        }

        if (walkable == 0)
            return new Report(false, 0, 0, false, roads, "No walkable tiles.");

        // Largest connected walkable component + does it touch edge?
        var seen = new bool[total];
        int largest = 0;
        bool largestTouchesEdge = false;

        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            int idx = x + y * n;
            if (seen[idx]) continue;

            if (!IsWalkable(m, x, y))
            {
                seen[idx] = true;
                continue;
            }

            int size = 0;
            bool touchesEdge = false;

            var q = new Queue<(int x, int y)>();
            q.Enqueue((x, y));
            seen[idx] = true;

            while (q.Count > 0)
            {
                var p = q.Dequeue();
                size++;

                if (p.x == 0 || p.y == 0 || p.x == n - 1 || p.y == n - 1)
                    touchesEdge = true;

                Try(p.x + 1, p.y);
                Try(p.x - 1, p.y);
                Try(p.x, p.y + 1);
                Try(p.x, p.y - 1);
            }

            if (size > largest)
            {
                largest = size;
                largestTouchesEdge = touchesEdge;
            }

            void Try(int xx, int yy)
            {
                if ((uint)xx >= (uint)n || (uint)yy >= (uint)n) return;
                int ii = xx + yy * n;
                if (seen[ii]) return;

                if (!IsWalkable(m, xx, yy))
                {
                    seen[ii] = true;
                    return;
                }

                seen[ii] = true;
                q.Enqueue((xx, yy));
            }
        }

        // Heuristics tuned for 128x128 (16384 tiles)
        int minLargest = Math.Max(600, total / 20); // >= 5% or 600

        if (largest < minLargest)
            return new Report(false, walkable, largest, largestTouchesEdge, roads,
                $"Largest walkable region too small ({largest} < {minLargest}).");

        // Touching edge is a great proxy for "not trapped in a bowl"
        if (!largestTouchesEdge)
            return new Report(false, walkable, largest, false, roads,
                "Largest region does not touch edge (likely trapped pocket).");

        // If it's a town, we expect at least some roads
        if (purpose == LocalMapPurpose.Town)
        {
            int minRoad = Math.Max(30, total / 200); // ~0.5% or 30
            if (roads < minRoad)
                return new Report(false, walkable, largest, largestTouchesEdge, roads,
                    $"Town has too few road tiles ({roads} < {minRoad}).");
        }

        return new Report(true, walkable, largest, largestTouchesEdge, roads, "OK");
    }
}
