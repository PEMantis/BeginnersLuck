namespace BeginnersLuck.Game.State;

public sealed class WorldState
{
    public int WorldSeed { get; set; } = 777;
    public WorldTravelState Travel { get; } = new();
    // Later:
    // public string SaveId { get; set; } = "dev";
    // public int WorldSize { get; set; } = 512;
    // public int ChunkSize { get; set; } = 32;
}
