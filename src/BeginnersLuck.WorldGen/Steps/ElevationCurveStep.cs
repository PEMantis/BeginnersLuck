using BeginnersLuck.WorldGen.Generation.Pipeline;

namespace BeginnersLuck.WorldGen.Steps;

public sealed class ElevationCurveStep : IWorldGenStep
{
    public string Name => "ElevationCurve";

    public void Run(WorldGenContext context)
    {
        var s = context.Request.Settings;

        // Build elevation histogram for percentile lookups
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

        int mountainStart = PercentileToValue(hist, total, s.MountainStartPercentile);
        context.Set("MountainStartElevation", mountainStart);

        // Apply curve in-place to elevation
        foreach (var (cx, cy) in context.Map.AllChunkCoords())
        {
            var chunk = context.Map.GetChunk(cx, cy);
            for (int i = 0; i < chunk.Elevation.Length; i++)
            {
                float v = chunk.Elevation[i] / 255f;

                // Gamma curve: emphasizes highs, compresses lows
                v = MathF.Pow(v, s.ElevationGamma);

                // Extra boost for the top band (mountains), smooth ramp
                if (chunk.Elevation[i] >= mountainStart)
                {
                    float t = (chunk.Elevation[i] - mountainStart) / (255f - mountainStart);
                    t = SmoothStep01(t);
                    v = MathF.Min(1f, v + (t * s.MountainBoost));
                }

                chunk.Elevation[i] = (byte)Math.Clamp((int)(v * 255f), 0, 255);
            }
        }
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

    private static float SmoothStep01(float x)
    {
        x = Math.Clamp(x, 0f, 1f);
        return x * x * (3f - 2f * x);
    }
}
