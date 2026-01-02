namespace BeginnersLuck.WorldGen.Generation.Pipeline;

public sealed class WorldGenPipeline
{
    private readonly List<IWorldGenStep> _steps = new();

    public WorldGenPipeline Add(IWorldGenStep step)
    {
        _steps.Add(step);
        return this;
    }

    public void Run(WorldGenContext ctx)
    {
        foreach (var step in _steps)
            step.Run(ctx);
    }
}
