using BeginnersLuck.WorldGen.Data;
using BeginnersLuck.WorldGen.Generation.Pipeline;

namespace BeginnersLuck.WorldGen.Steps;

public sealed class WaterStep : IWorldGenStep
{
    public string Name => "Water";

    public void Run(WorldGenContext context)
    {
        // Compute sea level by percentile using a histogram (fast, memory-light).
        int[] hist = new int[256];
        long total = 0;

        foreach (var (cx, cy) in context.Map.AllChunkCoords())
        {
            var chunk = context.Map.GetChunk(cx, cy);
            foreach (var e in chunk.Elevation)
            {
                hist[e]++;
                total++;
            }
        }

        var waterPercent = context.Request.Settings.WaterPercent;
        long targetWaterTiles = (long)Math.Round(waterPercent * total);

        long cumulative = 0;
        int seaLevel = 0;
        for (int i = 0; i < 256; i++)
        {
            cumulative += hist[i];
            if (cumulative >= targetWaterTiles)
            {
                seaLevel = i;
                break;
            }
        }
        
        context.Set("SeaLevel", seaLevel);

        // Optional: split water into shallow/deep based on distance below sea level.
        int deepCutoff = Math.Max(0, seaLevel - 18); // tweak later

        foreach (var (cx, cy) in context.Map.AllChunkCoords())
        {
            var chunk = context.Map.GetChunk(cx, cy);
            for (int idx = 0; idx < chunk.Elevation.Length; idx++)
            {
                byte e = chunk.Elevation[idx];

                if (e <= seaLevel)
                {
                    chunk.Terrain[idx] = (e <= deepCutoff) ? TileId.DeepWater : TileId.ShallowWater;
                }
                // else leave as Unknown and let TerrainStep classify land.
            }
        }

        // (Optional later) mark coast flags after terrain is fully assigned, using neighbor checks.
    }
}
