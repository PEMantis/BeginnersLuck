using BeginnersLuck.WorldGen.Generation.Pipeline;

namespace BeginnersLuck.WorldGen.Steps;

public sealed class ElevationStep : IWorldGenStep
{
    public string Name => "Elevation";

    public void Run(WorldGenContext context)
    {
        foreach (var (cx, cy) in context.Map.AllChunkCoords())
        {
            var chunk = context.Map.GetChunk(cx, cy);
            var seed = context.SeedFor(Name, cx, cy);
            var rng = new Random(seed);
            rng.NextBytes(chunk.Elevation);
        }
    }
}
