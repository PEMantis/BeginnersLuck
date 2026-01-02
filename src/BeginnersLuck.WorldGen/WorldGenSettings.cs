namespace BeginnersLuck.WorldGen;

public sealed record WorldGenSettings
{
    public int GeneratorVersion { get; init; } = 1;

    public float WaterPercent { get; init; } = 0.55f;

    // knobs for later steps
    public int RiverCount { get; init; } = 8;
    public int TownCount { get; init; } = 12;
}
