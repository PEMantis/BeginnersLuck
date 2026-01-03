using BeginnersLuck.WorldGen.Data;
using BeginnersLuck.WorldGen.Generation.Pipeline;

namespace BeginnersLuck.WorldGen.Steps;

public sealed class TownPlacementStep : IWorldGenStep
{
    public string Name => "TownsV2";

    public void Run(WorldGenContext ctx)
    {
        int w = ctx.Map.Width;
        int h = ctx.Map.Height;
        int cs = ctx.Map.ChunkSize;

        int seaLevel = ctx.Get<int>("SeaLevel");
        var s = ctx.Request.Settings;

        int targetTotal = Math.Max(1, s.TownCount);
        int minDist = Math.Max(1, s.TownMinDistance);
        int minDist2 = minDist * minDist;

        // Clear existing Town flags (so regen is clean)
        ClearTownFlags(ctx);

        // Prepare lookup tables
        int regionCount = ctx.TryGet<int>("RegionCount", out var rc) ? rc : EstimateMaxRegion(ctx, w, h, cs);
        int subCount = ctx.TryGet<int>("SubRegionCount", out var sc) ? sc : EstimateMaxSubRegion(ctx, w, h, cs);

        // Count area per region/subregion (land only)
        var regionArea = new int[regionCount + 1];
        var subArea = new int[subCount + 1];

        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            if (IsWater(ctx, x, y, cs, seaLevel)) continue;

            ushort r = GetRegion(ctx, x, y, cs);
            ushort sr = GetSubRegion(ctx, x, y, cs);
            if (r <= regionCount) regionArea[r]++;
            if (sr <= subCount) subArea[sr]++;
        }

        // Decide which subregions are "large enough" to require a town
        // Rule: large = at least 60% of target subregion size (tunable)
        int targetSubTiles = Math.Max(1000, s.SubRegionTargetTileCount);
        int largeSubThreshold = (int)(targetSubTiles * 0.60f);

        // Candidate generation stride (speed vs quality)
        int stride = 2;

        // Track picked towns
        var towns = new List<Point2i>(targetTotal);

        // ---- Pass 1: Guarantee 1 town per Region ----
        for (ushort regionId = 1; regionId <= regionCount; regionId++)
        {
            if (regionArea[regionId] <= 0) continue;
            if (towns.Count >= targetTotal) break;

            var best = FindBestTownCandidate(ctx, w, h, cs, seaLevel, stride, minDist2, towns,
                mustMatchRegion: regionId, mustMatchSubRegion: 0,
                allowBadBiomes: true); // if region is harsh, still place something

            if (best.score > int.MinValue)
            {
                PlaceTown(ctx, best.x, best.y, cs);
                towns.Add(new Point2i(best.x, best.y));
            }
        }

        // ---- Pass 2: Guarantee towns for large SubRegions ----
        for (ushort subId = 1; subId <= subCount; subId++)
        {
            if (towns.Count >= targetTotal) break;
            if (subArea[subId] < largeSubThreshold) continue;

            // Skip if already has a town
            if (SubRegionHasTown(ctx, w, h, cs, subId, towns))
                continue;

            var best = FindBestTownCandidate(ctx, w, h, cs, seaLevel, stride, minDist2, towns,
                mustMatchRegion: 0, mustMatchSubRegion: subId,
                allowBadBiomes: false);

            // If no good biome spots found, relax
            if (best.score == int.MinValue)
                best = FindBestTownCandidate(ctx, w, h, cs, seaLevel, stride, minDist2, towns,
                    mustMatchRegion: 0, mustMatchSubRegion: subId,
                    allowBadBiomes: true);

            if (best.score > int.MinValue)
            {
                PlaceTown(ctx, best.x, best.y, cs);
                towns.Add(new Point2i(best.x, best.y));
            }
        }

        // ---- Pass 3: Fill remaining globally by score ----
        while (towns.Count < targetTotal)
        {
            var best = FindBestTownCandidate(ctx, w, h, cs, seaLevel, stride, minDist2, towns,
                mustMatchRegion: 0, mustMatchSubRegion: 0,
                allowBadBiomes: false);

            // If we can't find any more "good" biome towns, relax
            if (best.score == int.MinValue)
            {
                best = FindBestTownCandidate(ctx, w, h, cs, seaLevel, stride, minDist2, towns,
                    mustMatchRegion: 0, mustMatchSubRegion: 0,
                    allowBadBiomes: true);
            }

            if (best.score == int.MinValue)
                break; // no space left

            PlaceTown(ctx, best.x, best.y, cs);
            towns.Add(new Point2i(best.x, best.y));
        }

        ctx.Set("Towns", towns);
        ctx.Set("TownCountPlaced", towns.Count);
    }

    // ---------- Candidate scoring ----------

    private static (int x, int y, int score) FindBestTownCandidate(
        WorldGenContext ctx,
        int w,
        int h,
        int cs,
        int seaLevel,
        int stride,
        int minDist2,
        List<Point2i> existing,
        ushort mustMatchRegion,
        ushort mustMatchSubRegion,
        bool allowBadBiomes)
    {
        var s = ctx.Request.Settings;

        int bestX = 0, bestY = 0;
        int bestScore = int.MinValue;

        for (int y = 0; y < h; y += stride)
        for (int x = 0; x < w; x += stride)
        {
            if (IsWater(ctx, x, y, cs, seaLevel)) continue;

            if (mustMatchRegion != 0 && GetRegion(ctx, x, y, cs) != mustMatchRegion) continue;
            if (mustMatchSubRegion != 0 && GetSubRegion(ctx, x, y, cs) != mustMatchSubRegion) continue;

            if (TooCloseToExisting(x, y, existing, minDist2)) continue;

            var biome = GetBiome(ctx, x, y, cs);

            if (!allowBadBiomes)
            {
                if (biome is BiomeId.Mountains or BiomeId.Hills) continue;
                if (biome is BiomeId.Swamp) continue;
                // Desert is allowed but discouraged; keep it but low score
            }

            int score = ScoreTown(ctx, x, y, w, h, cs, biome);

            // If we disallow bad biomes but this is desert, penalize heavily
            if (!allowBadBiomes && biome == BiomeId.Desert) score -= 40;

            // Deterministic jitter
            score += new Random(SeedI(ctx, "TownJitter", x, y)).Next(0, 25);

            if (score > bestScore)
            {
                bestScore = score;
                bestX = x;
                bestY = y;
            }
        }

        return (bestX, bestY, bestScore);
    }

    private static int ScoreTown(WorldGenContext ctx, int x, int y, int w, int h, int cs, BiomeId biome)
    {
        var s = ctx.Request.Settings;

        int score = 0;

        // Coast bias
        if ((GetFlags(ctx, x, y, cs) & TileFlags.Coast) != 0)
            score += s.TownCoastBias;

        // River density bias (this is the "confluence-ish" heuristic)
        int river = CountNearbyFlag(ctx, x, y, w, h, cs, TileFlags.River, radius: 8);
        score += river * 3; // density converts to points

        // Extra bump if we're very close to river
        if (river > 0) score += s.TownRiverBias / 3;

        // Plains bias / biome weighting
        if (biome == BiomeId.Plains) score += s.TownPlainsBias;
        if (biome == BiomeId.Forest) score += s.TownPlainsBias / 2;
        if (biome == BiomeId.Desert) score -= 15;
        if (biome == BiomeId.Swamp) score -= 35;

        // Prefer gentle terrain: avoid huge elevation spikes
        // (If you later add a slope map, use that. For now, local elevation variance.)
        score -= LocalRuggedness(ctx, x, y, w, h, cs) * 2;

        return score;
    }

    // ---------- Helpers ----------

    private static bool TooCloseToExisting(int x, int y, List<Point2i> existing, int minDist2)
    {
        foreach (var t in existing)
        {
            int dx = x - t.X;
            int dy = y - t.Y;
            if ((dx * dx + dy * dy) < minDist2) return true;
        }
        return false;
    }

    private static bool SubRegionHasTown(WorldGenContext ctx, int w, int h, int cs, ushort subId, List<Point2i> towns)
    {
        foreach (var t in towns)
        {
            if (GetSubRegion(ctx, t.X, t.Y, cs) == subId)
                return true;
        }
        return false;
    }

    private static int CountNearbyFlag(WorldGenContext ctx, int x, int y, int w, int h, int cs, TileFlags flag, int radius)
    {
        int r = Math.Max(1, radius);
        int count = 0;

        for (int dy = -r; dy <= r; dy++)
        for (int dx = -r; dx <= r; dx++)
        {
            // diamond
            if (Math.Abs(dx) + Math.Abs(dy) > r) continue;

            int px = x + dx;
            int py = y + dy;
            if ((uint)px >= (uint)w || (uint)py >= (uint)h) continue;

            if ((GetFlags(ctx, px, py, cs) & flag) != 0)
                count++;
        }

        return count;
    }

    private static int LocalRuggedness(WorldGenContext ctx, int x, int y, int w, int h, int cs)
    {
        // crude: max elevation delta within radius 2
        int r = 2;
        byte min = 255;
        byte max = 0;

        for (int dy = -r; dy <= r; dy++)
        for (int dx = -r; dx <= r; dx++)
        {
            int px = x + dx;
            int py = y + dy;
            if ((uint)px >= (uint)w || (uint)py >= (uint)h) continue;

            byte e = GetElevation(ctx, px, py, cs);
            if (e < min) min = e;
            if (e > max) max = e;
        }

        return max - min;
    }

    private static void PlaceTown(WorldGenContext ctx, int x, int y, int cs)
    {
        OrFlag(ctx, x, y, cs, TileFlags.Town);
    }

    private static void ClearTownFlags(WorldGenContext ctx)
    {
        foreach (var (cx, cy) in ctx.Map.AllChunkCoords())
        {
            var c = ctx.Map.GetChunk(cx, cy);
            for (int i = 0; i < c.Flags.Length; i++)
                c.Flags[i] &= ~TileFlags.Town;
        }
    }

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

    private static ushort GetSubRegion(WorldGenContext ctx, int x, int y, int cs)
    {
        var c = ctx.Map.GetChunk(x / cs, y / cs);
        return c.SubRegion[c.Index(x % cs, y % cs)];
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

    private static byte GetElevation(WorldGenContext ctx, int x, int y, int cs)
    {
        var c = ctx.Map.GetChunk(x / cs, y / cs);
        return c.Elevation[c.Index(x % cs, y % cs)];
    }

    private static void OrFlag(WorldGenContext ctx, int x, int y, int cs, TileFlags f)
    {
        var c = ctx.Map.GetChunk(x / cs, y / cs);
        c.Flags[c.Index(x % cs, y % cs)] |= f;
    }

    private static int SeedI(WorldGenContext ctx, string key, int x, int y)
        => unchecked((int)ctx.SeedFor(key, x, y));

    private static int EstimateMaxRegion(WorldGenContext ctx, int w, int h, int cs)
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

    private static int EstimateMaxSubRegion(WorldGenContext ctx, int w, int h, int cs)
    {
        ushort max = 0;
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            ushort r = GetSubRegion(ctx, x, y, cs);
            if (r > max) max = r;
        }
        return max;
    }
}
