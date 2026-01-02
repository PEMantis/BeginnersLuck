namespace BeginnersLuck.WorldGen.Generation;

public interface IWorldGenerator
{
    WorldMap Generate(WorldGenRequest request);
}
