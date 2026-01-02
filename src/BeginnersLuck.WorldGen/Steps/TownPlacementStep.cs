using BeginnersLuck.WorldGen.Data;
using BeginnersLuck.WorldGen.Generation.Pipeline;

namespace BeginnersLuck.WorldGen.Steps;

public sealed class TownPlacementStep : IWorldGenStep
{
    public string Name => "Towns";

    public void Run(WorldGenContext ctx)
    {
        int w = ctx.Map.Width;
        int h = ctx.Map.Height;
        int cs = ctx.Map.ChunkSize;

        int seaLevel = ctx.Get<int>("SeaLevel");
        var s = ctx.Request.Settings;

        int target = Math.Max(1, s.TownCount);
        int minDist2 = Math.Max(1, s.TownMinDistance) * Math.Max(1, s.TownMinDistance);

        // Candidate list: keep it manageable by sampling a grid stride
        int stride = 2; // tweak later for speed/quality tradeoff
        var candidates = new List<(int x, int y, int score)>(w * h / (stride * stride));

        for (int y = 0; y < h; y += stride)
        for (int x = 0; x < w; x += stride)
        {
            if (IsWater(ctx, x, y, cs, seaLevel)) continue;

            var biome = GetBiome(ctx, x, y, cs);
            if (biome is BiomeId.Mountains or BiomeId.Hills) continue; // towns avoid mountains early on

            int score = 0;

            // Coast bias
            if ((GetFlags(ctx, x, y, cs) & TileFlags.Coast) != 0)
                score += s.TownCoastBias;

            // River bias: within radius 6
            if (HasNearbyFlag(ctx, x, y, w, h, cs, TileFlags.River, 6))
                score += s.TownRiverBias;

            // Plains bias
            if (biome == BiomeId.Plains) score += s.TownPlainsBias;
            if (biome == BiomeId.Forest) score += (s.TownPlainsBias / 2); // forests ok but slightly less ideal

            // Avoid swamps/desert a bit
            if (biome == BiomeId.Swamp) score -= 25;
            if (biome == BiomeId.Desert) score -= 15;

            // Deterministic jitter so scores don't tie into a line
            score += new Random(ctx.SeedFor("TownJitter", x, y)).Next(0, 30);

            if (score > 0)
                candidates.Add((x, y, score));
        }

        candidates.Sort((a, b) => b.score.CompareTo(a.score));

        var towns = new List<Point2i>(target);

        foreach (var c in candidates)
        {
            if (towns.Count >= target) break;

            bool tooClose = false;
            foreach (var t in towns)
            {
                int dx = c.x - t.X;
                int dy = c.y - t.Y;
                if ((dx * dx + dy * dy) < minDist2)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose) continue;

            towns.Add(new Point2i(c.x, c.y));
            OrFlag(ctx, c.x, c.y, cs, TileFlags.Town);
        }

        ctx.Set("Towns", towns);
    }

    private static bool IsWater(WorldGenContext ctx, int x, int y, int cs, int seaLevel)
    {
        var c = ctx.Map.GetChunk(x / cs, y / cs);
        int i = c.Index(x % cs, y % cs);
        return c.Elevation[i] <= seaLevel || c.Terrain[i] is TileId.DeepWater or TileId.ShallowWater;
    }

    private static BiomeId GetBiome(WorldGenContext ctx, int x, int y, int cs)
    {
        var c = ctx.Map.GetChunk(x / cs, y / cs);
        return c.Biome[c.Index(x % cs, y % cs)];
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

    private static bool HasNearbyFlag(WorldGenContext ctx, int x, int y, int w, int h, int cs, TileFlags flag, int radius)
    {
        int r = Math.Max(1, radius);
        for (int dy = -r; dy <= r; dy++)
        for (int dx = -r; dx <= r; dx++)
        {
            if (Math.Abs(dx) + Math.Abs(dy) > r) continue;
            int px = x + dx;
            int py = y + dy;
            if ((uint)px >= (uint)w || (uint)py >= (uint)h) continue;

            if ((GetFlags(ctx, px, py, cs) & flag) != 0)
                return true;
        }
        return false;
    }
}
