public sealed record WorldGenSettings
{
    public int GeneratorVersion { get; init; } = 1;

    public float WaterPercent { get; init; } = 0.55f;

    // Elevation noise knobs (starter defaults)
    public float ElevationFrequency { get; init; } = 0.0035f;
    public int ElevationOctaves { get; init; } = 5;
    public float ElevationLacunarity { get; init; } = 2.0f;
    public float ElevationGain { get; init; } = 0.5f;

    public int RiverCount { get; init; } = 8;

    public int ShallowWaterBand { get; init; } = 4; // tiles from land into water
                                                    // Elevation shaping
    public float ElevationGamma { get; init; } = 1.20f;        // >1 lifts highs, flattens lows
    public int MountainStartPercentile { get; init; } = 85;    // 0..100
    public float MountainBoost { get; init; } = 0.18f;         // 0..1 extra lift for top band
    // River sources
    public int RiverSourceCount { get; init; } = 12;
    public int RiverSourceMinDistance { get; init; } = 28;     // tiles
    public int RiverSourceMinElevationPercentile { get; init; } = 80;
    public int RiverSourceMinMoisturePercentile { get; init; } = 55;
    // Rivers (carving)
    public int RiverMaxLength { get; init; } = 900;
    public int RiverMinLength { get; init; } = 120;
    public float RiverMeander { get; init; } = 0.35f;          // 0..1 (higher = wigglier)
    public int RiverWidthStart { get; init; } = 2;             // tiles
    public int RiverWidthEnd { get; init; } = 4;               // tiles near mouth

    // 1) Valley erosion
    public int RiverErosionRadius { get; init; } = 3;      // tiles
    public int RiverErosionStrength { get; init; } = 6;    // elevation drop at center (falls off)

    // 2) Biomes
    public int MountainElevationPercentile { get; init; } = 90;
    public int HillElevationPercentile { get; init; } = 75;

    // 3) Regions
    public bool IncludeIslandsAsRegions { get; init; } = true;

    // 4) Towns
    public int TownCount { get; init; } = 18;
    public int TownMinDistance { get; init; } = 42;        // tiles
    public int TownCoastBias { get; init; } = 40;          // score points
    public int TownRiverBias { get; init; } = 55;          // score points
    public int TownPlainsBias { get; init; } = 35;         // score points
                                                           // Sub-regions
    public int SubRegionTargetTileCount { get; init; } = 9000;   // ~how big each subregion is
    public int SubRegionMinSeedsPerRegion { get; init; } = 3;
    public int SubRegionMaxSeedsPerRegion { get; init; } = 10;


}
