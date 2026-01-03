using BeginnersLuck.WorldGen.Data;
using BeginnersLuck.WorldGen.Generation.Pipeline;

namespace BeginnersLuck.WorldGen.Steps;

public sealed class SubRegionStep : IWorldGenStep
{
    public string Name => "SubRegions";

    public void Run(WorldGenContext ctx)
    {
        int w = ctx.Map.Width;
        int h = ctx.Map.Height;
        int cs = ctx.Map.ChunkSize;

        foreach (var (cx, cy) in ctx.Map.AllChunkCoords())
        {
            var c = ctx.Map.GetChunk(cx, cy);
            Array.Clear(c.SubRegion);
        }

        int regionCount = ctx.TryGet<int>("RegionCount", out var rc) ? rc : EstimateRegionCount(ctx, w, h, cs);

        ushort nextSubId = 1;

        for (ushort regionId = 1; regionId <= regionCount; regionId++)
        {
            var tiles = CollectRegionTiles(ctx, regionId, w, h, cs);
            if (tiles.Count == 0) continue;

            int seedCount = ComputeSeedCount(ctx, tiles.Count);
            var seeds = PickSeeds(ctx, tiles, w, h, seedCount);

            var owner = new int[w * h];
            Array.Fill(owner, -1);

            var q = new Queue<int>(tiles.Count / 4);

            for (int i = 0; i < seeds.Count; i++)
            {
                int idx = seeds[i].y * w + seeds[i].x;
                owner[idx] = i;
                q.Enqueue(idx);
            }

            while (q.Count > 0)
            {
                int cur = q.Dequeue();
                int cxp = cur % w;
                int cyp = cur / w;
                int curOwner = owner[cur];

                TryVisit(cxp - 1, cyp, curOwner);
                TryVisit(cxp + 1, cyp, curOwner);
                TryVisit(cxp, cyp - 1, curOwner);
                TryVisit(cxp, cyp + 1, curOwner);
            }

            var seedToSubId = new ushort[seeds.Count];
            for (int i = 0; i < seeds.Count; i++)
                seedToSubId[i] = nextSubId++;

            foreach (int idx in tiles)
            {
                int o = owner[idx];
                if (o < 0) continue;
                int x = idx % w;
                int y = idx / w;
                SetSubRegion(ctx, x, y, cs, seedToSubId[o]);
            }

            void TryVisit(int nx, int ny, int curOwner)
            {
                if ((uint)nx >= (uint)w || (uint)ny >= (uint)h) return;

                int nidx = ny * w + nx;
                if (owner[nidx] != -1) return;

                if (GetRegion(ctx, nx, ny, cs) != regionId) return;

                owner[nidx] = curOwner;
                q.Enqueue(nidx);
            }
        }

        ctx.Set("SubRegionCount", (int)(nextSubId - 1));
    }

    private static int ComputeSeedCount(WorldGenContext ctx, int regionTileCount)
    {
        var s = ctx.Request.Settings;

        int ideal = Math.Max(1, regionTileCount / Math.Max(1, s.SubRegionTargetTileCount));
        ideal = Math.Clamp(ideal, s.SubRegionMinSeedsPerRegion, s.SubRegionMaxSeedsPerRegion);
        return ideal;
    }

    private static List<int> CollectRegionTiles(WorldGenContext ctx, ushort regionId, int w, int h, int cs)
    {
        var list = new List<int>(4096);

        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            if (GetRegion(ctx, x, y, cs) == regionId)
                list.Add(y * w + x);
        }

        return list;
    }

    private static List<(int x, int y)> PickSeeds(WorldGenContext ctx, List<int> tiles, int w, int h, int seedCount)
    {
        var seeds = new List<(int x, int y)>(seedCount);

        int bestIdx = tiles[0];
        int bestScore = int.MinValue;

        foreach (int idx in tiles)
        {
            int x = idx % w;
            int y = idx / w;

            int seed = unchecked((int)ctx.SeedFor("SubSeed0", x, y));
            int score = new Random(seed).Next(0, 1_000_000);

            if (score > bestScore)
            {
                bestScore = score;
                bestIdx = idx;
            }
        }

        seeds.Add((bestIdx % w, bestIdx / w));

        for (int k = 1; k < seedCount; k++)
        {
            int best = tiles[0];
            double bestD2 = -1;

            foreach (int idx in tiles)
            {
                int x = idx % w;
                int y = idx / w;

                double minD2 = double.PositiveInfinity;
                foreach (var s in seeds)
                {
                    int dx = x - s.x;
                    int dy = y - s.y;
                    double d2 = (dx * dx) + (dy * dy);
                    if (d2 < minD2) minD2 = d2;
                }

                int baseSeed = unchecked((int)ctx.SeedFor("SubSeedJ", x, y));
                int mixed = SeedMix(baseSeed, k);
                minD2 += new Random(mixed).Next(0, 250);

                if (minD2 > bestD2)
                {
                    bestD2 = minD2;
                    best = idx;
                }
            }

            seeds.Add((best % w, best / w));
        }

        return seeds;
    }

    private static int SeedMix(int seed, int extra)
    {
        unchecked
        {
            int x = seed;

            // 0x9E3779B9 overflows int, so force it as an int in unchecked context
            x ^= extra * unchecked((int)0x9E3779B9);

            x ^= (x << 13);
            x ^= (x >> 17);
            x ^= (x << 5);
            return x;
        }
    }


    private static ushort GetRegion(WorldGenContext ctx, int x, int y, int cs)
    {
        var c = ctx.Map.GetChunk(x / cs, y / cs);
        return c.Region[c.Index(x % cs, y % cs)];
    }

    private static void SetSubRegion(WorldGenContext ctx, int x, int y, int cs, ushort id)
    {
        var c = ctx.Map.GetChunk(x / cs, y / cs);
        c.SubRegion[c.Index(x % cs, y % cs)] = id;
    }

    private static int EstimateRegionCount(WorldGenContext ctx, int w, int h, int cs)
    {
        ushort max = 0;
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            ushort r = GetRegion(ctx, x, y, cs);
            if (r > max) max = r;
        }
        return max;
    }
}
