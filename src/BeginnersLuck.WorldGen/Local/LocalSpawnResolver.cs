using BeginnersLuck.WorldGen.Data;

namespace BeginnersLuck.WorldGen.Local;

public enum SpawnIntent { EnterFromRoad, EnterTownCenter, EnterFromPortal, FallbackSafe }
public enum Dir { North, East, South, West }

public sealed record SpawnRequest(
    SpawnIntent Intent,
    Dir? IncomingDir = null,
    string? PortalId = null
);

public static class LocalSpawnResolver
{
    public static Cell Resolve(LocalMap m, SpawnRequest req)
    {
        // Portal
        if (req.Intent == SpawnIntent.EnterFromPortal &&
            req.PortalId != null &&
            m.Meta.PortalAnchors.TryGetValue(req.PortalId, out var portal))
            return FindNearest(m, portal, preferRoad: false);

        // Town center
        if (req.Intent == SpawnIntent.EnterTownCenter && m.Meta.TownCenterCell is Cell tc)
            return FindNearest(m, tc, preferRoad: true);

        // Road entry (uses edge seed + prefers TileFlags.Road if present)
        if (req.Intent == SpawnIntent.EnterFromRoad && req.IncomingDir is Dir dir)
            return FindNearest(m, EdgeSeed(m.Size, dir), preferRoad: true);

        // Fallback
        return FindNearest(m, new Cell(m.Size / 2, m.Size / 2), preferRoad: false);
    }

    private static Cell EdgeSeed(int size, Dir dir) => dir switch
    {
        Dir.West  => new Cell(1, size / 2),
        Dir.East  => new Cell(size - 2, size / 2),
        Dir.North => new Cell(size / 2, 1),
        Dir.South => new Cell(size / 2, size - 2),
        _         => new Cell(size / 2, size / 2),
    };

    private static Cell FindNearest(LocalMap m, Cell start, bool preferRoad, int maxRadius = 96)
    {
        if (preferRoad)
        {
            var p = Find(m, start, maxRadius, requireRoad: true);
            if (p.HasValue) return p.Value;
        }

        return Find(m, start, maxRadius, requireRoad: false) ?? start;
    }

    private static Cell? Find(LocalMap m, Cell start, int maxRadius, bool requireRoad)
    {
        bool Ok(int x, int y)
        {
            if ((uint)x >= (uint)m.Size || (uint)y >= (uint)m.Size) return false;

            // Walkability: plug in your real rule here (example: block Water)
            var id = m.Terrain[m.Index(x, y)];
            bool walkable = LocalMapWalk.IsWalkable(m, x, y);
            if (!walkable) return false;

            if (requireRoad)
            {
                var flags = m.Flags[m.Index(x, y)];
                if ((flags & TileFlags.Road) == 0) return false;
            }

            return true;
        }

        if (Ok(start.X, start.Y)) return start;

        for (int r = 1; r <= maxRadius; r++)
        {
            int minX = start.X - r, maxX = start.X + r;
            int minY = start.Y - r, maxY = start.Y + r;

            for (int x = minX; x <= maxX; x++)
            {
                if (Ok(x, minY)) return new Cell(x, minY);
                if (Ok(x, maxY)) return new Cell(x, maxY);
            }

            for (int y = minY; y <= maxY; y++)
            {
                if (Ok(minX, y)) return new Cell(minX, y);
                if (Ok(maxX, y)) return new Cell(maxX, y);
            }
        }

        for (int y = 0; y < m.Size; y++)
        for (int x = 0; x < m.Size; x++)
            if (Ok(x, y))
                return new Cell(x, y);

        return null;
    }
}
