using BeginnersLuck.WorldGen.Data;

namespace BeginnersLuck.WorldGen.Local.Steps;

public sealed class LocalRiverStep : ILocalGenStep
{
    public string Name => "LocalRivers";

    public void Run(LocalGenContext ctx)
    {
        var p = ctx.Portals;

        var endpoints = new List<(int x, int y)>(4);
        int n = ctx.Map.Size;

        if (p.RiverN) endpoints.Add((p.RiverNPos, 0));
        if (p.RiverS) endpoints.Add((p.RiverSPos, n - 1));
        if (p.RiverW) endpoints.Add((0, p.RiverWPos));
        if (p.RiverE) endpoints.Add((n - 1, p.RiverEPos));

        if (endpoints.Count == 0)
        {
            // If the world tile has river but no neighbors, still create an internal river for flavor
            // (e.g., source tile). If you don't want this, just return.
            // We'll do a short river that heads downhill to nearest edge.
            if (!WorldTileHas(ctx, TileFlags.River)) return;

            var r = new Random(ctx.SeedFor("LocalSoloRiver"));
            int sx = r.Next(n / 4, 3 * n / 4);
            int sy = r.Next(n / 4, 3 * n / 4);

            var bestEdge = PickDownhillEdge(ctx, sx, sy);
            endpoints.Add((sx, sy));
            endpoints.Add(bestEdge);
        }

        // Pair endpoints:
        // - If 2 endpoints: connect them.
        // - If >2: connect all to a central confluence point (nice look).
        if (endpoints.Count == 2)
        {
            CarveRiver(ctx, endpoints[0], endpoints[1], widthStart: 2, widthEnd: 3);
        }
        else
        {
            var center = ComputeCenter(endpoints, n);
            foreach (var e in endpoints)
                CarveRiver(ctx, e, center, widthStart: 2, widthEnd: 3);
        }
    }

    private static (int x, int y) ComputeCenter(List<(int x, int y)> pts, int n)
    {
        int sx = 0, sy = 0;
        foreach (var p in pts) { sx += p.x; sy += p.y; }
        int cx = sx / pts.Count;
        int cy = sy / pts.Count;
        cx = Math.Clamp(cx, n / 4, 3 * n / 4);
        cy = Math.Clamp(cy, n / 4, 3 * n / 4);
        return (cx, cy);
    }

    private static void CarveRiver(LocalGenContext ctx, (int x, int y) a, (int x, int y) b, int widthStart, int widthEnd)
    {
        int n = ctx.Map.Size;
        int maxSteps = n * 4;

        int seed = ctx.SeedFor("RiverCarve", a.x * 1024 + a.y, b.x * 1024 + b.y);
        var r = new Random(seed);

        int x = a.x, y = a.y;
        for (int step = 0; step < maxSteps; step++)
        {
            float t = step / (float)maxSteps;
            int w = (int)MathF.Round(widthStart + (widthEnd - widthStart) * t);
            StampRiver(ctx, x, y, Math.Clamp(w, 1, 4));

            if (x == b.x && y == b.y) break;

            // biased step toward goal + jitter
            int dx = Math.Sign(b.x - x);
            int dy = Math.Sign(b.y - y);

            // occasionally favor perpendicular wiggle
            if (r.NextDouble() < 0.25)
            {
                if (r.NextDouble() < 0.5) dx = 0;
                else dy = 0;
            }

            int nx = Math.Clamp(x + dx, 0, n - 1);
            int ny = Math.Clamp(y + dy, 0, n - 1);

            // bias downhill if possible
            (nx, ny) = DownhillBias(ctx, x, y, nx, ny, r);

            x = nx; y = ny;
        }
    }

    private static (int x, int y) DownhillBias(LocalGenContext ctx, int x, int y, int nx, int ny, Random r)
    {
        int n = ctx.Map.Size;
        byte e0 = ctx.Map.Elevation[ctx.Map.Index(x, y)];

        // try a few candidates around intended step
        (int x, int y) best = (nx, ny);
        byte bestE = ctx.Map.Elevation[ctx.Map.Index(nx, ny)];

        for (int i = 0; i < 3; i++)
        {
            int cx = Math.Clamp(nx + r.Next(-1, 2), 0, n - 1);
            int cy = Math.Clamp(ny + r.Next(-1, 2), 0, n - 1);
            byte e = ctx.Map.Elevation[ctx.Map.Index(cx, cy)];
            if (e <= bestE)
            {
                bestE = e;
                best = (cx, cy);
            }
        }

        // only accept if it doesn't go sharply uphill
        if (bestE <= e0 + 3) return best;
        return (nx, ny);
    }

    private static void StampRiver(LocalGenContext ctx, int x, int y, int radius)
    {
        int n = ctx.Map.Size;
        for (int dy = -radius; dy <= radius; dy++)
        for (int dx = -radius; dx <= radius; dx++)
        {
            if (dx * dx + dy * dy > radius * radius) continue;
            int px = x + dx;
            int py = y + dy;
            if ((uint)px >= (uint)n || (uint)py >= (uint)n) continue;

            int idx = ctx.Map.Index(px, py);

            ctx.Map.Flags[idx] |= TileFlags.River;

            // carve elevation a bit
            byte e = ctx.Map.Elevation[idx];
            int ne = e - 6;
            if (ne < 0) ne = 0;
            ctx.Map.Elevation[idx] = (byte)ne;

            // convert shallow areas to shallow water for visual
            if (ctx.Map.Elevation[idx] <= ctx.SeaLevel + 2)
                ctx.Map.Terrain[idx] = TileId.ShallowWater;
        }
    }

    private static bool WorldTileHas(LocalGenContext ctx, TileFlags f)
    {
        int wx = ctx.Request.WorldX;
        int wy = ctx.Request.WorldY;
        int cs = ctx.World.ChunkSize;

        var c = ctx.World.GetChunk(wx / cs, wy / cs);
        int idx = c.Index(wx % cs, wy % cs);
        return (c.Flags[idx] & f) != 0;
    }

    private static (int x, int y) PickDownhillEdge(LocalGenContext ctx, int sx, int sy)
    {
        int n = ctx.Map.Size;
        // evaluate 4 edges by minimum elevation along that edge
        int best = 0;
        byte bestE = 255;

        byte nE = EdgeMin(ctx, Edge.North);
        byte sE = EdgeMin(ctx, Edge.South);
        byte wE = EdgeMin(ctx, Edge.West);
        byte eE = EdgeMin(ctx, Edge.East);

        (best, bestE) = MinPick(0, nE, best, bestE);
        (best, bestE) = MinPick(2, sE, best, bestE);
        (best, bestE) = MinPick(3, wE, best, bestE);
        (best, bestE) = MinPick(1, eE, best, bestE);

        // choose portal position for that edge
        int pos = new Random(ctx.SeedFor("SoloRiverEdge")).Next(n / 6, 5 * n / 6);

        return best switch
        {
            0 => (pos, 0),
            2 => (pos, n - 1),
            3 => (0, pos),
            _ => (n - 1, pos),
        };

        byte EdgeMin(LocalGenContext c, Edge edge)
        {
            byte min = 255;
            if (edge == Edge.North) { for (int x = 0; x < n; x++) min = Math.Min(min, c.Map.Elevation[c.Map.Index(x, 0)]); }
            if (edge == Edge.South) { for (int x = 0; x < n; x++) min = Math.Min(min, c.Map.Elevation[c.Map.Index(x, n - 1)]); }
            if (edge == Edge.West)  { for (int y = 0; y < n; y++) min = Math.Min(min, c.Map.Elevation[c.Map.Index(0, y)]); }
            if (edge == Edge.East)  { for (int y = 0; y < n; y++) min = Math.Min(min, c.Map.Elevation[c.Map.Index(n - 1, y)]); }
            return min;
        }

        static (int best, byte bestE) MinPick(int edge, byte e, int best, byte bestE)
            => e < bestE ? (edge, e) : (best, bestE);
    }
}
