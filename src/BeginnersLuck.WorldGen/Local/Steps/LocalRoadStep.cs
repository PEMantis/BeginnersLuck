using BeginnersLuck.WorldGen.Data;

namespace BeginnersLuck.WorldGen.Local.Steps;

public sealed class LocalRoadStep : ILocalGenStep
{
    public string Name => "LocalRoads";

    public void Run(LocalGenContext ctx)
    {
        var p = ctx.Portals;

        var endpoints = new List<(int x, int y)>(4);
        int n = ctx.Map.Size;

        if (p.RoadN) endpoints.Add((p.RoadNPos, 0));
        if (p.RoadS) endpoints.Add((p.RoadSPos, n - 1));
        if (p.RoadW) endpoints.Add((0, p.RoadWPos));
        if (p.RoadE) endpoints.Add((n - 1, p.RoadEPos));

        bool hasTownCenter = ctx.TryGet("TownCenter", out Point2i center);

        if (endpoints.Count == 0 && !hasTownCenter)
            return;

        // If town: connect all road portals to town center
        if (hasTownCenter)
        {
            foreach (var e in endpoints)
                CarveRoad(ctx, e, (center.X, center.Y));

            // If no portals, create a couple “local streets” for debug vibes
            if (endpoints.Count == 0)
            {
                CarveRoad(ctx, (0, n / 2), (center.X, center.Y));
                CarveRoad(ctx, (n - 1, n / 2), (center.X, center.Y));
            }

            return;
        }

        // Wilderness roads: connect endpoints pairwise
        if (endpoints.Count == 2)
        {
            CarveRoad(ctx, endpoints[0], endpoints[1]);
        }
        else if (endpoints.Count > 2)
        {
            var mid = (n / 2, n / 2);
            foreach (var e in endpoints)
                CarveRoad(ctx, e, mid);
        }
    }

    private static void CarveRoad(LocalGenContext ctx, (int x, int y) a, (int x, int y) b)
    {
        int n = ctx.Map.Size;
        int seed = ctx.SeedFor("RoadCarve", a.x * 1024 + a.y, b.x * 1024 + b.y);
        var r = new Random(seed);

        int x = a.x, y = a.y;
        int maxSteps = n * 4;

        for (int step = 0; step < maxSteps; step++)
        {
            StampRoad(ctx, x, y);

            if (x == b.x && y == b.y) break;

            int dx = Math.Sign(b.x - x);
            int dy = Math.Sign(b.y - y);

            // less wiggly than rivers, but still organic
            if (r.NextDouble() < 0.12)
            {
                if (r.NextDouble() < 0.5) dx = 0;
                else dy = 0;
            }

            int nx = Math.Clamp(x + dx, 0, n - 1);
            int ny = Math.Clamp(y + dy, 0, n - 1);

            // avoid deep water strongly
            int idx = ctx.Map.Index(nx, ny);
            if (ctx.Map.Terrain[idx] == TileId.DeepWater)
            {
                // sidestep
                if (dx != 0) ny = Math.Clamp(y + (r.NextDouble() < 0.5 ? -1 : 1), 0, n - 1);
                else nx = Math.Clamp(x + (r.NextDouble() < 0.5 ? -1 : 1), 0, n - 1);
            }

            x = nx; y = ny;
        }
    }

    private static void StampRoad(LocalGenContext ctx, int x, int y)
    {
        int idx = ctx.Map.Index(x, y);
        ctx.Map.Flags[idx] |= TileFlags.Road;

        // roads "prefer" buildable land: convert swamp to dirt, keep rivers as is
        if (ctx.Map.Terrain[idx] == TileId.Swamp)
            ctx.Map.Terrain[idx] = TileId.Dirt;
    }
}
