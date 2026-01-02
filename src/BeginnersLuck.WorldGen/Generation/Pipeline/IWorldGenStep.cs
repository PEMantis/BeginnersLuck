namespace BeginnersLuck.WorldGen.Generation.Pipeline;

public interface IWorldGenStep
{
    string Name { get; }
    void Run(WorldGenContext context);
}
