using BeginnersLuck.WorldGen.Data;
using BeginnersLuck.WorldGen.Generation.Pipeline;

namespace BeginnersLuck.WorldGen.Steps;

public sealed class BiomeStep : IWorldGenStep
{
    public string Name => "Biomes";

    public void Run(WorldGenContext ctx)
    {
        int seaLevel = ctx.Get<int>("SeaLevel");
        var s = ctx.Request.Settings;

        // Elevation thresholds by percentile
        var (hillE, mountainE) = ComputeHillMountainThresholds(ctx, s.HillElevationPercentile, s.MountainElevationPercentile);

        foreach (var (cx, cy) in ctx.Map.AllChunkCoords())
        {
            var chunk = ctx.Map.GetChunk(cx, cy);

            for (int i = 0; i < chunk.Biome.Length; i++)
            {
                byte e = chunk.Elevation[i];
                byte m = chunk.Moisture[i];
                byte t = chunk.Temperature[i];
                var terrain = chunk.Terrain[i];
                var flags = chunk.Flags[i];

                // Water first
                if (e <= seaLevel || terrain is TileId.DeepWater or TileId.ShallowWater)
                {
                    chunk.Biome[i] = ((flags & TileFlags.Coast) != 0) ? BiomeId.Coast : BiomeId.Ocean;
                    continue;
                }

                // Mountains / hills by elevation threshold
                if (e >= mountainE) { chunk.Biome[i] = BiomeId.Mountains; continue; }
                if (e >= hillE)     { chunk.Biome[i] = BiomeId.Hills; continue; }

                // Cold biomes
                if (t < 45) chunk.Biome[i] = (m > 140) ? BiomeId.Tundra : BiomeId.Snow;
                else if (t < 70) chunk.Biome[i] = (m > 150) ? BiomeId.Forest : BiomeId.Plains;
                else
                {
                    // Warm biomes
                    if (m < 55) chunk.Biome[i] = BiomeId.Desert;
                    else if (m > 200) chunk.Biome[i] = BiomeId.Swamp;
                    else chunk.Biome[i] = BiomeId.Plains;
                }

                // Optional: mark Forest flag for later rendering/gameplay
                if (chunk.Biome[i] == BiomeId.Forest)
                    chunk.Flags[i] |= TileFlags.Forest;
            }
        }

        ctx.Set("HillElevation", (int)hillE);
        ctx.Set("MountainElevation", (int)mountainE);
    }

    private static (byte hill, byte mountain) ComputeHillMountainThresholds(WorldGenContext ctx, int hillPct, int mountainPct)
    {
        int[] hist = new int[256];
        long total = 0;

        foreach (var (cx, cy) in ctx.Map.AllChunkCoords())
        {
            var c = ctx.Map.GetChunk(cx, cy);
            foreach (var e in c.Elevation) { hist[e]++; total++; }
        }

        byte hill = (byte)PercentileToValue(hist, total, hillPct);
        byte mountain = (byte)PercentileToValue(hist, total, mountainPct);

        if (mountain < hill) mountain = hill;
        return (hill, mountain);
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
