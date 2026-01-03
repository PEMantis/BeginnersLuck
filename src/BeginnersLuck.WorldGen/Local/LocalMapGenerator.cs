using BeginnersLuck.WorldGen.Data;
using BeginnersLuck.WorldGen.Local.Steps;

namespace BeginnersLuck.WorldGen.Local;

public interface ILocalMapGenerator
{
    LocalMap Generate(WorldMap world, LocalMapRequest request);
}

public sealed class LocalMapGenerator : ILocalMapGenerator
{
    public LocalMap Generate(WorldMap world, LocalMapRequest request)
    {
        int size = Math.Clamp(request.Size, 32, 512);
        int localSeed = DeriveLocalSeed(request.Seed, request.WorldX, request.WorldY);

        var map = new LocalMap(size, localSeed, request.WorldX, request.WorldY);
        var ctx = new LocalGenContext(world, request with { Size = size, Seed = localSeed }, map);

        // Read world tile metadata and decide sea level (IMPORTANT)
        ReadWorldTile(ctx);

        // Run local pipeline
        var steps = new ILocalGenStep[]
        {
            new LocalFieldsStep(),
            new LocalTerrainStep(),
            new LocalPortalStep(),
            new LocalRiverStep(),
            new LocalTownStep(),
            new LocalRoadStep(),
        };

        foreach (var step in steps)
            step.Run(ctx);

        return map;
    }

    private static int DeriveLocalSeed(int worldSeed, int wx, int wy)
    {
        unchecked
        {
            int s = worldSeed;
            s ^= wx * 73856093;
            s ^= wy * 19349663;

            // Important: constant overflows int; force it to int deterministically
            s ^= unchecked((int)0x9E3779B9);

            s ^= (s << 13);
            s ^= (s >> 17);
            s ^= (s << 5);
            return s;
        }
    }

    private static void ReadWorldTile(LocalGenContext ctx)
    {
        int wx = ctx.Request.WorldX;
        int wy = ctx.Request.WorldY;
        int cs = ctx.World.ChunkSize;

        var chunk = ctx.World.GetChunk(wx / cs, wy / cs);
        int idx = chunk.Index(wx % cs, wy % cs);

        ctx.Biome = chunk.Biome[idx];
        ctx.Region = chunk.Region[idx];
        ctx.SubRegion = chunk.SubRegion[idx];
        ctx.IsTownTile = (chunk.Flags[idx] & TileFlags.Town) != 0;

        // ✅ Sea level is chosen ONLY from world TILE TERRAIN.
        // Inland tiles should NOT auto-flood.
        // Coastal/ocean tiles should have shoreline water.
        var worldTerrain = chunk.Terrain[idx];

        ctx.SeaLevel = worldTerrain switch
        {
            TileId.DeepWater    => 145,
            TileId.ShallowWater => 135,
            TileId.Coast        => 130,
            _                   => 80,   // <-- critical: inland wilderness default
        };
    }

    public LocalGenContext GenerateWithContext(WorldMap world, LocalMapRequest request)
    {
        int size = Math.Clamp(request.Size, 32, 512);
        int localSeed = DeriveLocalSeed(request.Seed, request.WorldX, request.WorldY);

        var map = new LocalMap(size, localSeed, request.WorldX, request.WorldY);
        var ctx = new LocalGenContext(world, request with { Size = size, Seed = localSeed }, map);

        ReadWorldTile(ctx);

        var steps = new ILocalGenStep[]
 {
    new LocalFieldsStep(),
    new LocalTerrainStep(),
    new LocalPortalStep(),

    // NEW: fills in “empty wilderness” with biome-appropriate features
    new LocalBiomeFeatureStep(),

    new LocalRiverStep(),
    new LocalTownStep(),
    new LocalRoadStep(),
 };


        foreach (var step in steps)
            step.Run(ctx);

        return ctx;
    }

}
