using BeginnersLuck.WorldGen.Data;

namespace BeginnersLuck.WorldGen.Local.Steps;

public sealed class LocalFieldsStep : ILocalGenStep
{
    public string Name => "LocalFields";

    public void Run(LocalGenContext ctx)
    {
        int n = ctx.Map.Size;
        int seed = ctx.Map.Seed;

        // Biome shaping knobs (0..1 space)
        (float baseElev, float elevAmp, float moistBias, float tempBias, float roughness) =
            BiomeProfile(ctx.Biome, ctx.Request.Purpose);

        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float nx = x / (float)n;
            float ny = y / (float)n;

            float e = Fbm(seed + 101, nx, ny, 5, 2.0f, 0.5f);
            float m = Fbm(seed + 202, nx, ny, 4, 2.1f, 0.55f);
            float t = Fbm(seed + 303, nx, ny, 3, 2.2f, 0.60f);

            // Shape elevation
            e = baseElev + (e * elevAmp);
            e = Mix(e, (e * roughness), 0.35f);

            // Moist/temp biases
            m = Clamp01(m + moistBias);
            t = Clamp01(t + tempBias);

            int idx = ctx.Map.Index(x, y);

            ctx.Map.Elevation[idx] = (byte)Math.Clamp((int)(e * 255f), 0, 255);
            ctx.Map.Moisture[idx] = (byte)Math.Clamp((int)(m * 255f), 0, 255);
            ctx.Map.Temperature[idx] = (byte)Math.Clamp((int)(t * 255f), 0, 255);
        }

        // ✅ IMPORTANT:
        // Do NOT set ctx.SeaLevel here.
        // SeaLevel is decided by LocalMapGenerator.ReadWorldTile using world terrain.
    }

    private static (float baseElev, float elevAmp, float moistBias, float tempBias, float roughness)
        BiomeProfile(BiomeId b, LocalMapPurpose purpose)
    {
        // Raised base elevation for land so inland tiles don't end up near sea level.
        return b switch
        {
            BiomeId.Ocean     => (0.15f, 0.10f, 0.10f, 0.00f, 0.70f),
            BiomeId.Coast     => (0.35f, 0.18f, 0.08f, 0.00f, 0.80f),

            BiomeId.Plains    => (0.62f, 0.18f, 0.05f, 0.05f, 0.85f),
            BiomeId.Forest    => (0.64f, 0.20f, 0.18f, 0.02f, 0.90f),
            BiomeId.Desert    => (0.66f, 0.22f, -0.25f, 0.15f, 0.95f),
            BiomeId.Swamp     => (0.60f, 0.16f, 0.28f, 0.05f, 0.85f),

            BiomeId.Hills     => (0.70f, 0.28f, 0.02f, -0.02f, 1.05f),
            BiomeId.Mountains => (0.78f, 0.34f, -0.02f, -0.06f, 1.20f),

            BiomeId.Tundra    => (0.64f, 0.22f, 0.05f, -0.25f, 0.95f),
            BiomeId.Snow      => (0.66f, 0.24f, 0.02f, -0.35f, 1.00f),

            _ => (0.62f, 0.20f, 0.00f, 0.00f, 0.90f)
        };
    }

    private static float Mix(float a, float b, float t) => a + (b - a) * t;
    private static float Clamp01(float v) => v < 0 ? 0 : (v > 1 ? 1 : v);

    // Hash-based smooth value noise
    private static float Hash01(int seed, int x, int y)
    {
        unchecked
        {
            int h = seed;
            h ^= x * 374761393;
            h ^= y * 668265263;
            h = (h ^ (h >> 13)) * 1274126177;
            h ^= (h >> 16);
            return (h & 0x00FFFFFF) / 16777215f;
        }
    }

    private static float Smooth(float t) => t * t * (3f - 2f * t);

    private static float ValueNoise(int seed, float x, float y)
    {
        int x0 = (int)MathF.Floor(x);
        int y0 = (int)MathF.Floor(y);
        int x1 = x0 + 1;
        int y1 = y0 + 1;

        float tx = Smooth(x - x0);
        float ty = Smooth(y - y0);

        float a = Hash01(seed, x0, y0);
        float b = Hash01(seed, x1, y0);
        float c = Hash01(seed, x0, y1);
        float d = Hash01(seed, x1, y1);

        float ab = a + (b - a) * tx;
        float cd = c + (d - c) * tx;
        return ab + (cd - ab) * ty;
    }

    private static float Fbm(int seed, float nx, float ny, int octaves, float lacunarity, float gain)
    {
        float amp = 1f;
        float freq = 1f;
        float sum = 0f;
        float norm = 0f;

        for (int i = 0; i < octaves; i++)
        {
            sum += ValueNoise(seed + i * 1013, nx * 8f * freq, ny * 8f * freq) * amp;
            norm += amp;
            amp *= gain;
            freq *= lacunarity;
        }

        return sum / Math.Max(0.0001f, norm);
    }
}
