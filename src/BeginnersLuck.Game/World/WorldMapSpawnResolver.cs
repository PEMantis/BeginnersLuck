using System;
using System.Collections.Generic;
using BeginnersLuck.Engine.World;
using Microsoft.Xna.Framework;

namespace BeginnersLuck.Game.World;

public static class WorldSpawnResolver
{
    public readonly record struct RegionInfo(
        int Size,
        bool TouchesEdge);

    /// <summary>
    /// Finds a walkable spawn that isn't trapped in a pocket.
    /// Strategy:
    /// - Search candidates outward from preferred point.
    /// - Candidate must be walkable (not solid).
    /// - Candidate's connected walkable region must be "good":
    ///     - big enough
    ///     - touches edge (proxy for "not sealed")  OR can be disabled if you want.
    /// </summary>
    public static Point FindPlayableSpawn(
        TileMap map,
        Point preferred,
        int maxSearchRadius,
        int minRegionSize,
        bool requireTouchesEdge = true)
    {
        if (!map.IsSolidCell(preferred.X, preferred.Y))
        {
            var info0 = ProbeRegion(map, preferred, minRegionSize, requireTouchesEdge);
            if (IsGood(info0, minRegionSize, requireTouchesEdge))
                return preferred;
        }

        // Expanding "square ring" search
        for (int r = 1; r <= maxSearchRadius; r++)
        {
            int minX = preferred.X - r;
            int maxX = preferred.X + r;
            int minY = preferred.Y - r;
            int maxY = preferred.Y + r;

            // Top + Bottom edges of the ring
            for (int x = minX; x <= maxX; x++)
            {
                if (TryCandidate(x, minY, out var p)) return p;
                if (TryCandidate(x, maxY, out p)) return p;
            }

            // Left + Right edges of the ring
            for (int y = minY + 1; y <= maxY - 1; y++)
            {
                if (TryCandidate(minX, y, out var p)) return p;
                if (TryCandidate(maxX, y, out p)) return p;
            }
        }

        // If all else fails: nearest non-solid (can still be trapped, but at least walkable)
        return FindNearestWalkable(map, preferred);

        bool TryCandidate(int x, int y, out Point found)
        {
            found = default;

            if ((uint)x >= (uint)map.Width || (uint)y >= (uint)map.Height)
                return false;

            if (map.IsSolidCell(x, y))
                return false;

            var start = new Point(x, y);
            var info = ProbeRegion(map, start, minRegionSize, requireTouchesEdge);

            if (!IsGood(info, minRegionSize, requireTouchesEdge))
                return false;

            found = start;
            return true;
        }
    }

    private static bool IsGood(RegionInfo info, int minRegionSize, bool requireTouchesEdge)
    {
        if (info.Size < minRegionSize) return false;
        if (requireTouchesEdge && !info.TouchesEdge) return false;
        return true;
    }

    /// <summary>
    /// Flood-fill from start on walkable cells, with early-outs:
    /// - If requireTouchesEdge==false, we can early-out once size >= minRegionSize.
    /// - If requireTouchesEdge==true, we early-out once (size >= minRegionSize AND touches edge).
    /// </summary>
    private static RegionInfo ProbeRegion(TileMap map, Point start, int minRegionSize, bool requireTouchesEdge)
    {
        if (map.IsSolidCell(start.X, start.Y))
            return new RegionInfo(0, false);

        var q = new Queue<Point>();
        var seen = new HashSet<int>();

        q.Enqueue(start);
        seen.Add(map.Index(start.X, start.Y));

        int size = 0;
        bool touchesEdge = false;

        while (q.Count > 0)
        {
            var p = q.Dequeue();
            size++;

            if (p.X == 0 || p.Y == 0 || p.X == map.Width - 1 || p.Y == map.Height - 1)
                touchesEdge = true;

            // Early-outs
            if (!requireTouchesEdge && size >= minRegionSize)
                return new RegionInfo(size, touchesEdge);

            if (requireTouchesEdge && touchesEdge && size >= minRegionSize)
                return new RegionInfo(size, true);

            Try(p.X + 1, p.Y);
            Try(p.X - 1, p.Y);
            Try(p.X, p.Y + 1);
            Try(p.X, p.Y - 1);
        }

        return new RegionInfo(size, touchesEdge);

        void Try(int x, int y)
        {
            if ((uint)x >= (uint)map.Width || (uint)y >= (uint)map.Height) return;
            if (map.IsSolidCell(x, y)) return;

            int idx = map.Index(x, y);
            if (!seen.Add(idx)) return;

            q.Enqueue(new Point(x, y));
        }
    }

    private static Point FindNearestWalkable(TileMap map, Point start)
    {
        for (int r = 0; r < 512; r++)
        {
            for (int dy = -r; dy <= r; dy++)
            {
                int dx = r - Math.Abs(dy);

                var a = new Point(start.X + dx, start.Y + dy);
                if ((uint)a.X < (uint)map.Width && (uint)a.Y < (uint)map.Height && !map.IsSolidCell(a.X, a.Y))
                    return a;

                var b = new Point(start.X - dx, start.Y + dy);
                if ((uint)b.X < (uint)map.Width && (uint)b.Y < (uint)map.Height && !map.IsSolidCell(b.X, b.Y))
                    return b;
            }
        }

        return start;
    }
}
