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

        ReadWorldTile(ctx);

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
            new LocalBiomeFeatureStep(),
            new LocalRiverStep(),
            new LocalTownStep(),
            new LocalRoadStep(),
        };

        foreach (var step in steps)
            step.Run(ctx);

        return ctx;
    }

    private static int DeriveLocalSeed(int worldSeed, int wx, int wy)
    {
        unchecked
        {
            int s = worldSeed;
            s ^= wx * 73856093;
            s ^= wy * 19349663;
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

        var worldTerrain = chunk.Terrain[idx];

        // SeaLevel is a byte threshold (0..255) used by LocalTerrainStep.
        // For inland tiles, it must be LOW so we don't flood the whole map.
        //
        // Think of it like: "how high does the ocean reach into this local map?"
        // Inland wilderness: near-zero (no ocean).
        // Coast: some shoreline.
        // Ocean/deep water: lots of water.

        ctx.SeaLevel = worldTerrain switch
        {
            TileId.DeepWater    => 145, // mostly water
            TileId.Ocean        => 145,
            TileId.ShallowWater => 130, // water but more shoreline
            TileId.Coast        => 120, // noticeable coast
            _                   => 20,  // ✅ inland default: almost no sea
        };

        // If you WANT a tiny bit of ponds/lakes inland, do it in LocalTerrainStep
        // using a "lakes" noise mask, not by raising global sea level.
    }
}
