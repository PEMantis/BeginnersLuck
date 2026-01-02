using BeginnersLuck.WorldGen.Generation.Pipeline;
using BeginnersLuck.WorldGen.Noise;

namespace BeginnersLuck.WorldGen.Steps;

public sealed class ElevationStep : IWorldGenStep
{
    public string Name => "Elevation";

    public void Run(WorldGenContext context)
    {
        var s = context.Request.Settings;

        // Step seed is stable even if pipeline changes order
        int seed = context.SeedFor(Name);

        INoise2D noise = new FastNoiseLiteNoise2D(
            seed: seed,
            frequency: s.ElevationFrequency,
            octaves: s.ElevationOctaves,
            lacunarity: s.ElevationLacunarity,
            gain: s.ElevationGain
        );

        int w = context.Map.Width;
        int h = context.Map.Height;
        int cs = context.Map.ChunkSize;

        // Optional: a soft “continent mask” that reduces land near edges (helps avoid endless coastlines)
        // This is not required, but it makes early maps feel nicer.
        float cxWorld = (w - 1) * 0.5f;
        float cyWorld = (h - 1) * 0.5f;
        float maxDist = MathF.Sqrt(cxWorld * cxWorld + cyWorld * cyWorld);

        foreach (var (cx, cy) in context.Map.AllChunkCoords())
        {
            var chunk = context.Map.GetChunk(cx, cy);

            for (int ly = 0; ly < cs; ly++)
            for (int lx = 0; lx < cs; lx++)
            {
                int x = (cx * cs) + lx;
                int y = (cy * cs) + ly;

                // Base coherent noise: ~[-1,1]
                float n = noise.Sample(x, y);

                // Normalize to [0,1]
                float v = (n * 0.5f) + 0.5f;

                // Soft continent falloff (0 at edges-ish, 1 near center)
                float dx = x - cxWorld;
                float dy = y - cyWorld;
                float d = MathF.Sqrt(dx * dx + dy * dy) / maxDist; // 0..1
                float falloff = SmoothStep(1.0f, 0.55f, d);        // tweak 0.55..0.75 later

                // Blend: keep some noise everywhere but favor land towards center
                v = (v * 0.85f) + (falloff * 0.15f);

                // Convert to byte 0..255
                byte e = (byte)Math.Clamp((int)(v * 255f), 0, 255);

                chunk.Elevation[chunk.Index(lx, ly)] = e;
            }
        }
    }

    private static float SmoothStep(float edge0, float edge1, float x)
    {
        // standard smoothstep but with clamping
        x = Math.Clamp((x - edge0) / (edge1 - edge0), 0f, 1f);
        return x * x * (3f - 2f * x);
    }
}
