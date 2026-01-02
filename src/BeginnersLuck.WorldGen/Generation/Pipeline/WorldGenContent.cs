using BeginnersLuck.WorldGen.Data;

namespace BeginnersLuck.WorldGen.Generation.Pipeline;

public sealed class WorldGenContext
{
    public WorldGenRequest Request { get; }
    public WorldMap Map { get; }
    public int RootSeed => Request.Seed;

    public WorldGenContext(WorldGenRequest request, WorldMap map)
    {
        Request = request;
        Map = map;
    }

    public int SeedFor(string label, int a = 0, int b = 0) =>
        Seeds.Derive(Request.Seed, label, a, b);
}
