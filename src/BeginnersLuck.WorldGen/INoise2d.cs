namespace BeginnersLuck.WorldGen.Noise;

public interface INoise2D
{
    // Returns roughly [-1, 1] (depends on noise type/fractal)
    float Sample(float x, float y);
}
