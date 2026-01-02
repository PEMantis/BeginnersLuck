using BeginnersLuck.WorldGen.Data;
using BeginnersLuck.WorldGen.Generation.Pipeline;

namespace BeginnersLuck.WorldGen.Steps;

public sealed class RiverSourcesStep : IWorldGenStep
{
    public string Name => "RiverSources";

    public void Run(WorldGenContext context)
    {
        var s = context.Request.Settings;

        int w = context.Map.Width;
        int h = context.Map.Height;
        int cs = context.Map.ChunkSize;

        // Build histograms for percentile thresholds
        int[] elevHist = new int[256];
        int[] moistHist = new int[256];
        long total = 0;

        foreach (var (cx, cy) in context.Map.AllChunkCoords())
        {
            var chunk = context.Map.GetChunk(cx, cy);
            for (int i = 0; i < chunk.Elevation.Length; i++)
            {
                elevHist[chunk.Elevation[i]]++;
                moistHist[chunk.Moisture[i]]++;
                total++;
            }
        }

        int minElev = PercentileToValue(elevHist, total, s.RiverSourceMinElevationPercentile);
        int minMoist = PercentileToValue(moistHist, total, s.RiverSourceMinMoisturePercentile);

        // Also avoid water: use SeaLevel if available
        int seaLevel = context.TryGet<int>("SeaLevel", out var sl) ? sl : -1;

        // Collect candidates
        var candidates = new List<(int x, int y, int score)>(10000);

        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            var (e, m) = GetElevMoist(context, x, y, cs);

            if (seaLevel >= 0 && e <= seaLevel) continue; // must be land
            if (e < minElev) continue;
            if (m < minMoist) continue;

            // Score: favor high elevation + high moisture
            int score = (e * 3) + (m * 2);

            // Small deterministic jitter so ties don't stack in a line
            int jitterSeed = context.SeedFor("RiverSourceJitter", x, y);
            score += (new Random(jitterSeed).Next(0, 50));

            candidates.Add((x, y, score));
        }

        // Sort by best score first
        candidates.Sort((a, b) => b.score.CompareTo(a.score));

        // Pick with min distance constraint
        var picked = new List<Point2i>(s.RiverSourceCount);
        int minDist2 = s.RiverSourceMinDistance * s.RiverSourceMinDistance;

        foreach (var c in candidates)
        {
            if (picked.Count >= s.RiverSourceCount)
                break;

            bool tooClose = false;
            foreach (var p in picked)
            {
                int dx = c.x - p.X;
                int dy = c.y - p.Y;
                if ((dx * dx + dy * dy) < minDist2)
                {
                    tooClose = true;
                    break;
                }
            }

            if (tooClose) continue;

            picked.Add(new Point2i(c.x, c.y));
            OrFlag(context, c.x, c.y, cs, TileFlags.RiverSource);
        }

        context.Set("RiverSources", picked);
    }

    private static (byte elev, byte moist) GetElevMoist(WorldGenContext ctx, int x, int y, int cs)
    {
        int cx = x / cs; int cy = y / cs;
        int lx = x % cs; int ly = y % cs;
        var chunk = ctx.Map.GetChunk(cx, cy);
        int idx = chunk.Index(lx, ly);
        return (chunk.Elevation[idx], chunk.Moisture[idx]);
    }

    private static void OrFlag(WorldGenContext ctx, int x, int y, int cs, TileFlags flag)
    {
        int cx = x / cs; int cy = y / cs;
        int lx = x % cs; int ly = y % cs;
        var chunk = ctx.Map.GetChunk(cx, cy);
        int i = chunk.Index(lx, ly);
        chunk.Flags[i] |= flag;
    }

    private static int PercentileToValue(int[] hist, long total, int percentile)
    {
        percentile = Math.Clamp(percentile, 0, 100);
        long target = (long)Math.Round((percentile / 100f) * total);

        long cumulative = 0;
        for (int i = 0; i < 256; i++)
        {
            cumulative += hist[i];
            if (cumulative >= target)
                return i;
        }
        return 255;
    }
}
