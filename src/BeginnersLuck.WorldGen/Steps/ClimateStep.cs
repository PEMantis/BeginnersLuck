using BeginnersLuck.WorldGen.Generation.Pipeline;

namespace BeginnersLuck.WorldGen.Steps;

public sealed class ClimateStep : IWorldGenStep
{
    public string Name => "Climate";

    public void Run(WorldGenContext context)
    {
        foreach (var (cx, cy) in context.Map.AllChunkCoords())
        {
            var chunk = context.Map.GetChunk(cx, cy);

            var seedMoist = context.SeedFor("Moisture", cx, cy);
            var seedTemp  = context.SeedFor("Temperature", cx, cy);

            new Random(seedMoist).NextBytes(chunk.Moisture);
            new Random(seedTemp).NextBytes(chunk.Temperature);
        }
    }
}
