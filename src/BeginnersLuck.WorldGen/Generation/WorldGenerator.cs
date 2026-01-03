using BeginnersLuck.WorldGen.Data;
using BeginnersLuck.WorldGen.Generation.Pipeline;
using BeginnersLuck.WorldGen.Steps;

namespace BeginnersLuck.WorldGen.Generation;

public sealed class WorldGenerator : IWorldGenerator
{
    public WorldMap Generate(WorldGenRequest request)
    {
        Validate(request);

        var map = new WorldMap
        {
            Width = request.Width,
            Height = request.Height,
            ChunkSize = request.ChunkSize,
            Seed = request.Seed,
            GeneratorVersion = request.Settings.GeneratorVersion
        };

        // Allocate chunks first (so steps can assume they exist)
        foreach (var (cx, cy) in map.AllChunkCoords())
            map.SetChunk(cx, cy, new Chunk(request.ChunkSize, cx, cy));

        var ctx = new WorldGenContext(request, map);

        var pipeline = new WorldGenPipeline()
         .Add(new ElevationStep())
         .Add(new ElevationCurveStep())
         .Add(new ClimateStep())
         .Add(new WaterStep())
         .Add(new CoastStep())
         .Add(new TerrainStep())
         .Add(new RiverSourcesStep())
         .Add(new RiverStep())
        // 1) erosion improves elevation around rivers
        .Add(new ValleyErosionStep())

        // 2) biomes depend on final-ish elevation, moisture, temp, coast
        .Add(new BiomeStep())

        // 3) regions depend on final land/water
        .Add(new RegionStep())
        .Add(new SubRegionStep())
        // 4) towns depend on rivers, coast, biomes, regions
        .Add(new TownPlacementStep())
        .Add(new RoadStep());

        pipeline.Run(ctx);
        return map;
    }

    private static void Validate(WorldGenRequest r)
    {
        if (r.Width <= 0 || r.Height <= 0) throw new ArgumentOutOfRangeException("World size must be positive.");
        if (r.ChunkSize <= 0) throw new ArgumentOutOfRangeException("ChunkSize must be positive.");
        if (r.Width % r.ChunkSize != 0 || r.Height % r.ChunkSize != 0)
            throw new ArgumentException("Width/Height must be divisible by ChunkSize.");
        if (r.Settings.WaterPercent is < 0.01f or > 0.99f)
            throw new ArgumentOutOfRangeException(nameof(r.Settings.WaterPercent), "WaterPercent should be between 0.01 and 0.99.");
    }
}
