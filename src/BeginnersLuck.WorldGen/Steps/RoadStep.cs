using BeginnersLuck.WorldGen.Data;
using BeginnersLuck.WorldGen.Generation.Pipeline;

namespace BeginnersLuck.WorldGen.Steps;

public sealed class RoadStep : IWorldGenStep
{
    public string Name => "Roads";

    public void Run(WorldGenContext ctx)
    {
        if (!ctx.TryGet("Towns", out List<Point2i> towns) || towns.Count < 2)
            return;

        int w = ctx.Map.Width;
        int h = ctx.Map.Height;
        int cs = ctx.Map.ChunkSize;

        int seaLevel = ctx.Get<int>("SeaLevel");

        // Clear existing roads
        foreach (var (cx, cy) in ctx.Map.AllChunkCoords())
        {
            var c = ctx.Map.GetChunk(cx, cy);
            for (int i = 0; i < c.Flags.Length; i++)
                c.Flags[i] &= ~TileFlags.Road;
        }

        // Build edges: connect each town to its K nearest in the same Region,
        // plus one global nearest to keep islands from being totally disconnected (if possible).
        const int K = 2;

        var edges = new HashSet<(int a, int b)>();

        for (int i = 0; i < towns.Count; i++)
        {
            var a = towns[i];
            ushort ra = GetRegion(ctx, a.X, a.Y, cs);

            var nearest = new List<(int j, int d2)>(8);

            for (int j = 0; j < towns.Count; j++)
            {
                if (j == i) continue;

                var b = towns[j];
                ushort rb = GetRegion(ctx, b.X, b.Y, cs);
                if (rb != ra) continue;

                int dx = a.X - b.X;
                int dy = a.Y - b.Y;
                int d2 = dx * dx + dy * dy;
                nearest.Add((j, d2));
            }

            nearest.Sort((x, y) => x.d2.CompareTo(y.d2));

            for (int n = 0; n < Math.Min(K, nearest.Count); n++)
            {
                int j = nearest[n].j;
                AddEdge(edges, i, j);
            }

            // Fallback: connect to nearest town overall if this region has only one town
            if (nearest.Count == 0)
            {
                int bestJ = -1;
                int bestD2 = int.MaxValue;

                for (int j = 0; j < towns.Count; j++)
                {
                    if (j == i) continue;
                    var b = towns[j];
                    int dx = a.X - b.X;
                    int dy = a.Y - b.Y;
                    int d2 = dx * dx + dy * dy;
                    if (d2 < bestD2) { bestD2 = d2; bestJ = j; }
                }

                if (bestJ >= 0)
                    AddEdge(edges, i, bestJ);
            }
        }

        int roadsMade = 0;

        foreach (var (aIdx, bIdx) in edges)
        {
            var start = towns[aIdx];
            var goal = towns[bIdx];

            // If they are in different land regions, path will likely fail (ocean).
            // We'll just try; later we add ferries/bridges.
            if (TryFindPath(ctx, start, goal, w, h, cs, seaLevel, out var path))
            {
                MarkRoad(ctx, path, cs);
                roadsMade++;
            }
        }

        ctx.Set("RoadEdgeCount", edges.Count);
        ctx.Set("RoadPathsBuilt", roadsMade);
    }

    private static void AddEdge(HashSet<(int a, int b)> edges, int i, int j)
    {
        if (i == j) return;
        if (i < j) edges.Add((i, j));
        else edges.Add((j, i));
    }

    // ---------- A* ----------

    private static bool TryFindPath(
        WorldGenContext ctx,
        Point2i start,
        Point2i goal,
        int w,
        int h,
        int cs,
        int seaLevel,
        out List<Point2i> path)
    {
        path = new List<Point2i>();

        int startId = start.Y * w + start.X;
        int goalId = goal.Y * w + goal.X;

        var open = new PriorityQueue<int, int>();
        var cameFrom = new Dictionary<int, int>(8192);
        var gScore = new Dictionary<int, int>(8192);

        gScore[startId] = 0;
        open.Enqueue(startId, Heuristic(start.X, start.Y, goal.X, goal.Y));

        int iterations = 0;
        int maxIterations = w * h / 2; // guard

        while (open.Count > 0 && iterations++ < maxIterations)
        {
            int current = open.Dequeue();
            if (current == goalId)
            {
                Reconstruct(current, startId, cameFrom, w, path);
                return true;
            }

            int cxp = current % w;
            int cyp = current / w;

            // 4-neighbor movement keeps roads clean
            Span<(int x, int y)> n =
            [
                (cxp - 1, cyp),
                (cxp + 1, cyp),
                (cxp, cyp - 1),
                (cxp, cyp + 1)
            ];

            int curG = gScore[current];

            for (int i = 0; i < n.Length; i++)
            {
                int nx = n[i].x;
                int ny = n[i].y;
                if ((uint)nx >= (uint)w || (uint)ny >= (uint)h) continue;

                if (IsWater(ctx, nx, ny, cs, seaLevel)) continue; // no ocean roads

                int nid = ny * w + nx;

                int stepCost = MoveCost(ctx, cxp, cyp, nx, ny, cs);
                int tentative = curG + stepCost;

                if (!gScore.TryGetValue(nid, out int old) || tentative < old)
                {
                    cameFrom[nid] = current;
                    gScore[nid] = tentative;

                    int f = tentative + Heuristic(nx, ny, goal.X, goal.Y);
                    open.Enqueue(nid, f);
                }
            }
        }

        return false;
    }

    private static int Heuristic(int x1, int y1, int x2, int y2)
        => (Math.Abs(x1 - x2) + Math.Abs(y1 - y2)) * 10;

    private static int MoveCost(WorldGenContext ctx, int x, int y, int nx, int ny, int cs)
    {
        var aBiome = GetBiome(ctx, x, y, cs);
        var bBiome = GetBiome(ctx, nx, ny, cs);

        int cost = 10;

        // prefer plains-ish
        cost += BiomePenalty(bBiome);

        // ruggedness: elevation delta
        byte e1 = GetElevation(ctx, x, y, cs);
        byte e2 = GetElevation(ctx, nx, ny, cs);
        cost += Math.Abs(e2 - e1) * 2;

        // crossing rivers is expensive (bridges later)
        if ((GetFlags(ctx, nx, ny, cs) & TileFlags.River) != 0)
            cost += 80;

        return cost;
    }

    private static int BiomePenalty(BiomeId b) => b switch
    {
        BiomeId.Plains => 0,
        BiomeId.Forest => 6,
        BiomeId.Desert => 10,
        BiomeId.Swamp => 18,
        BiomeId.Hills => 25,
        BiomeId.Mountains => 60,
        BiomeId.Tundra => 8,
        BiomeId.Snow => 12,
        BiomeId.Coast => 2,
        _ => 5
    };

    private static void Reconstruct(int current, int start, Dictionary<int, int> cameFrom, int w, List<Point2i> path)
    {
        int cur = current;
        while (cur != start)
        {
            int x = cur % w;
            int y = cur / w;
            path.Add(new Point2i(x, y));
            cur = cameFrom[cur];
        }
        path.Add(new Point2i(start % w, start / w));
        path.Reverse();
    }

    // ---------- Mark road ----------

    private static void MarkRoad(WorldGenContext ctx, List<Point2i> path, int cs)
    {
        foreach (var p in path)
            OrFlag(ctx, p.X, p.Y, cs, TileFlags.Road);
    }

    // ---------- Data helpers ----------

    private static bool IsWater(WorldGenContext ctx, int x, int y, int cs, int seaLevel)
    {
        var c = ctx.Map.GetChunk(x / cs, y / cs);
        int i = c.Index(x % cs, y % cs);
        return c.Elevation[i] <= seaLevel || c.Terrain[i] is TileId.DeepWater or TileId.ShallowWater;
    }

    private static ushort GetRegion(WorldGenContext ctx, int x, int y, int cs)
    {
        var c = ctx.Map.GetChunk(x / cs, y / cs);
        return c.Region[c.Index(x % cs, y % cs)];
    }

    private static BiomeId GetBiome(WorldGenContext ctx, int x, int y, int cs)
    {
        var c = ctx.Map.GetChunk(x / cs, y / cs);
        return c.Biome[c.Index(x % cs, y % cs)];
    }

    private static byte GetElevation(WorldGenContext ctx, int x, int y, int cs)
    {
        var c = ctx.Map.GetChunk(x / cs, y / cs);
        return c.Elevation[c.Index(x % cs, y % cs)];
    }

    private static TileFlags GetFlags(WorldGenContext ctx, int x, int y, int cs)
    {
        var c = ctx.Map.GetChunk(x / cs, y / cs);
        return c.Flags[c.Index(x % cs, y % cs)];
    }

    private static void OrFlag(WorldGenContext ctx, int x, int y, int cs, TileFlags f)
    {
        var c = ctx.Map.GetChunk(x / cs, y / cs);
        c.Flags[c.Index(x % cs, y % cs)] |= f;
    }
}
