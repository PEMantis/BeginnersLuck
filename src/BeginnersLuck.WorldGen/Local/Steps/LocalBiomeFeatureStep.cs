using BeginnersLuck.WorldGen.Data;

namespace BeginnersLuck.WorldGen.Local.Steps;

public sealed class LocalBiomeFeatureStep : ILocalGenStep
{
    public string Name => "LocalBiomeFeatures";

    public void Run(LocalGenContext ctx)
    {
        // If portals already exist, we don't invent continuity features.
        bool hasRiverPortals = HasAnyRiverPortal(ctx.Portals);
        bool hasRoadPortals  = HasAnyRoadPortal(ctx.Portals);

        // Synthetic rivers/streams for wilderness tiles that would otherwise be empty.
        if (!hasRiverPortals)
        {
            TryAddSyntheticWater(ctx);
        }

        // Towns: optional “ensure entrances” if you want towns to always have paths.
        // Keep it subtle: only if Purpose == Town and zero road portals.
        if (ctx.Request.Purpose == LocalMapPurpose.Town && !hasRoadPortals)
        {
            EnsureTownEntrances(ctx);
        }
    }

    private static bool HasAnyRiverPortal(EdgePortals p) => p.RiverN || p.RiverE || p.RiverS || p.RiverW;
    private static bool HasAnyRoadPortal(EdgePortals p)  => p.RoadN  || p.RoadE  || p.RoadS  || p.RoadW;

    private static void TryAddSyntheticWater(LocalGenContext ctx)
    {
        int n = ctx.Map.Size;
        var r = new Random(ctx.SeedFor("SynthWater"));

        // Decide if we add anything at all (biome weighted).
        double chance = ctx.Biome switch
        {
            BiomeId.Swamp => 0.95,
            BiomeId.Forest => 0.55,
            BiomeId.Plains => 0.35,
            BiomeId.Hills => 0.40,
            BiomeId.Mountains => 0.45,
            BiomeId.Desert => 0.08,
            BiomeId.Tundra => 0.20,
            BiomeId.Snow => 0.15,
            _ => 0.25
        };

        if (r.NextDouble() > chance)
            return;

        // Pick 1-3 features
        int count = ctx.Biome == BiomeId.Swamp ? r.Next(2, 5) : r.Next(1, 3);

        for (int i = 0; i < count; i++)
        {
            if (ctx.Biome == BiomeId.Swamp && r.NextDouble() < 0.45)
            {
                // Pool
                int px = r.Next(n / 6, 5 * n / 6);
                int py = r.Next(n / 6, 5 * n / 6);
                StampPool(ctx, px, py, r.Next(6, 14));
            }
            else
            {
                // Stream: connect two internal points or edge-to-edge
                var a = RandomInterior(r, n);
                var b = RandomInterior(r, n);

                // For mountains/hills, bias to "downhill" feel by picking b on lower elevation side
                if (ctx.Biome is BiomeId.Mountains or BiomeId.Hills)
                {
                    var lo = PickLower(ctx, a, b);
                    b = lo;
                }

                CarveStream(ctx, a, b);
            }
        }
    }

    private static (int x, int y) RandomInterior(Random r, int n)
        => (r.Next(n / 8, 7 * n / 8), r.Next(n / 8, 7 * n / 8));

    private static (int x, int y) PickLower(LocalGenContext ctx, (int x, int y) a, (int x, int y) b)
    {
        byte ea = ctx.Map.Elevation[ctx.Map.Index(a.x, a.y)];
        byte eb = ctx.Map.Elevation[ctx.Map.Index(b.x, b.y)];
        return eb <= ea ? b : a;
    }

    private static void StampPool(LocalGenContext ctx, int cx, int cy, int radius)
    {
        int n = ctx.Map.Size;
        for (int y = cy - radius; y <= cy + radius; y++)
        for (int x = cx - radius; x <= cx + radius; x++)
        {
            if ((uint)x >= (uint)n || (uint)y >= (uint)n) continue;
            int dx = x - cx;
            int dy = y - cy;
            if (dx * dx + dy * dy > radius * radius) continue;

            int idx = ctx.Map.Index(x, y);
            ctx.Map.Flags[idx] |= TileFlags.River; // treat pools as river-water for now
            ctx.Map.Terrain[idx] = TileId.ShallowWater;

            // lower elevation slightly so it reads as basin
            int ne = ctx.Map.Elevation[idx] - 10;
            if (ne < 0) ne = 0;
            ctx.Map.Elevation[idx] = (byte)ne;
        }
    }

    private static void CarveStream(LocalGenContext ctx, (int x, int y) a, (int x, int y) b)
    {
        int n = ctx.Map.Size;
        int seed = ctx.SeedFor("SynthStream", a.x * 1024 + a.y, b.x * 1024 + b.y);
        var r = new Random(seed);

        int x = a.x, y = a.y;
        int maxSteps = n * 3;

        for (int step = 0; step < maxSteps; step++)
        {
            int width = (ctx.Biome == BiomeId.Swamp) ? 2 : 1;
            StampStream(ctx, x, y, width);

            if (x == b.x && y == b.y) break;

            int dx = Math.Sign(b.x - x);
            int dy = Math.Sign(b.y - y);

            if (r.NextDouble() < 0.22)
            {
                if (r.NextDouble() < 0.5) dx = 0;
                else dy = 0;
            }

            int nx = Math.Clamp(x + dx, 0, n - 1);
            int ny = Math.Clamp(y + dy, 0, n - 1);

            // downhill bias
            (nx, ny) = DownhillBias(ctx, x, y, nx, ny, r);

            x = nx; y = ny;
        }
    }

    private static (int x, int y) DownhillBias(LocalGenContext ctx, int x, int y, int nx, int ny, Random r)
    {
        int n = ctx.Map.Size;
        byte e0 = ctx.Map.Elevation[ctx.Map.Index(x, y)];

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

        return bestE <= e0 + 4 ? best : (nx, ny);
    }

    private static void StampStream(LocalGenContext ctx, int x, int y, int radius)
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

            // Swamp streams become shallow water more easily
            if (ctx.Biome == BiomeId.Swamp)
                ctx.Map.Terrain[idx] = TileId.ShallowWater;

            int ne = ctx.Map.Elevation[idx] - 6;
            if (ne < 0) ne = 0;
            ctx.Map.Elevation[idx] = (byte)ne;
        }
    }

    private static void EnsureTownEntrances(LocalGenContext ctx)
    {
        int n = ctx.Map.Size;
        int inset = Math.Max(3, n / 16);
        var r = new Random(ctx.SeedFor("TownEntrances"));

        // Two deterministic entrances on opposite sides
        int posA = r.Next(inset, n - inset);
        int posB = r.Next(inset, n - inset);

        ctx.Portals.RoadW = true; ctx.Portals.RoadWPos = posA;
        ctx.Portals.RoadE = true; ctx.Portals.RoadEPos = posB;
    }
}
