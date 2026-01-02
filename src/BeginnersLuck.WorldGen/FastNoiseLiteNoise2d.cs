using BeginnersLuck.WorldGen.Generation.Pipeline;

namespace BeginnersLuck.WorldGen.Noise;

public sealed class FastNoiseLiteNoise2D : INoise2D
{
    private readonly FastNoiseLite _noise;

    public FastNoiseLiteNoise2D(int seed, float frequency, int octaves, float lacunarity, float gain)
    {
        _noise = new FastNoiseLite(seed);

        // Good starter combo for terrain
        _noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        _noise.SetFractalType(FastNoiseLite.FractalType.FBm);

        _noise.SetFrequency(frequency);
        _noise.SetFractalOctaves(octaves);
        _noise.SetFractalLacunarity(lacunarity);
        _noise.SetFractalGain(gain);
    }

    public float Sample(float x, float y) => _noise.GetNoise(x, y);
}
