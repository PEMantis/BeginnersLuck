using System;
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

        var steps = request.Purpose switch
        {
            LocalMapPurpose.Town => new ILocalGenStep[]
            {
                new LocalFieldsStep(),
                new LocalTerrainStep(),
                new LocalPortalStep(),
                new LocalRiverStep(),
                new LocalTownStep(),
                new LocalRoadStep(),
            },

            LocalMapPurpose.Road => new ILocalGenStep[]
            {
                new LocalFieldsStep(),
                new LocalTerrainStep(),
                new LocalPortalStep(),
                new LocalRiverStep(),
                new LocalRoadStep(),
            },

            LocalMapPurpose.Ruins => new ILocalGenStep[]
            {
                new LocalFieldsStep(),
                new LocalTerrainStep(),
                new LocalPortalStep(),
                new LocalRiverStep(),
                new LocalRuinsStep(),
            },

            _ => new ILocalGenStep[]
            {
                new LocalFieldsStep(),
                new LocalTerrainStep(),
                new LocalPortalStep(),
                new LocalRiverStep(),
            }
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

        var steps = request.Purpose switch
        {
            LocalMapPurpose.Town => new ILocalGenStep[]
            {
                new LocalFieldsStep(),
                new LocalTerrainStep(),
                new LocalPortalStep(),
                new LocalBiomeFeatureStep(),
                new LocalRiverStep(),
                new LocalTownStep(),
                new LocalRoadStep(),
            },

            LocalMapPurpose.Road => new ILocalGenStep[]
            {
                new LocalFieldsStep(),
                new LocalTerrainStep(),
                new LocalPortalStep(),
                new LocalBiomeFeatureStep(),
                new LocalRiverStep(),
                new LocalRoadStep(),
            },

            LocalMapPurpose.Ruins => new ILocalGenStep[]
            {
                new LocalFieldsStep(),
                new LocalTerrainStep(),
                new LocalPortalStep(),
                new LocalBiomeFeatureStep(),
                new LocalRiverStep(),
                new LocalRuinsStep(),
            },

            _ => new ILocalGenStep[]
            {
                new LocalFieldsStep(),
                new LocalTerrainStep(),
                new LocalPortalStep(),
                new LocalBiomeFeatureStep(),
                new LocalRiverStep(),
            }
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

        ctx.SeaLevel = worldTerrain switch
        {
            TileId.DeepWater    => 145,
            TileId.Ocean        => 145,
            TileId.ShallowWater => 130,
            TileId.Coast        => 120,
            _                   => 20,
        };
    }
}
