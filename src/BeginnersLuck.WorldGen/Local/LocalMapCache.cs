using System;
using System.IO;
using BeginnersLuck.Engine.World;
using BeginnersLuck.Game.World;
using BeginnersLuck.WorldGen.Data;
using BeginnersLuck.WorldGen.Local;

namespace BeginnersLuck.WorldGen;

public static class LocalMapCache
{
    // Keep one generator instance
    private static readonly ILocalMapGenerator _gen = new LocalMapGenerator();

    public static string EnsureLocalExists(
        WorldMap world,
        int worldSeed,
        int wx,
        int wy,
        int localSize,
        LocalMapPurpose purpose)
    {
        string path = WorldPaths.LocalBin(worldSeed, wx, wy);

        // Existing?
        if (File.Exists(path))
        {
            var loaded = LocalMapBinLoader.Load(path);
            var rep = LocalMapValidator.Validate(loaded, purpose);

            if (rep.Playable)
                return path;

            Console.WriteLine($"[LocalMapCache] Existing local unplayable: {path}");
            Console.WriteLine($"[LocalMapCache] {rep.Reason} largest={rep.LargestRegion} edge={rep.LargestTouchesEdge} walkable={rep.WalkableCount} roads={rep.RoadCount}");
            TryDelete(path);
        }

        const int maxAttempts = 12;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            int attemptSeed = DeriveLocalSeed(worldSeed, wx, wy, (uint)attempt);

            // Generate using your real generator API
            var req = new LocalMapRequest(
                Seed: attemptSeed,
                WorldX: wx,
                WorldY: wy,
                Size: localSize,
                Purpose: purpose
            );

            // Generate with context so we can pull portals/biome/townCenter if your steps set them
            // If you don't want context, swap to _gen.Generate(world, req) and set defaults below.
            var ctx = new LocalMapGenerator().GenerateWithContext(world, req);

            var map = ctx.Map;

            // Pull what we can from context
            BiomeId biome = ctx.Biome;
            EdgePortals portals = ctx.Portals; // If LocalPortalStep writes ctx.Portals
            (int X, int Y)? townCenter = ctx.TownCenter; // If LocalTownStep writes ctx.TownCenter

            // If your ctx doesn't expose these fields, replace with safe defaults:
            // var portals = new EdgePortals();
            // (int X, int Y)? townCenter = null;

            var data = LocalMapDataAdapter.ToData(map, purpose, biome, portals, townCenter);

            LocalMapBinWriter.Save(path, data);

            var rep = LocalMapValidator.Validate(data, purpose);
            Console.WriteLine($"[LocalMapCache] Gen attempt {attempt} seed={attemptSeed} => playable={rep.Playable} largest={rep.LargestRegion} edge={rep.LargestTouchesEdge} roads={rep.RoadCount} reason={rep.Reason}");

            if (rep.Playable)
                return path;

            TryDelete(path);
        }

        Console.WriteLine($"[LocalMapCache] WARNING: Could not generate playable map for ({wx},{wy}) after {maxAttempts} attempts. Keeping last file: {path}");
        return path;
    }

    private static void TryDelete(string path)
    {
        try { File.Delete(path); }
        catch (Exception e) { Console.WriteLine($"[LocalMapCache] Failed to delete {path}: {e.Message}"); }
    }

    private static int DeriveLocalSeed(int worldSeed, int wx, int wy, uint attempt)
    {
        unchecked
        {
            uint h = (uint)worldSeed;
            h = HashStep(h ^ (uint)wx);
            h = HashStep(h ^ (uint)wy);
            h = HashStep(h ^ attempt);
            return (int)h;
        }
    }

    private static uint HashStep(uint x)
    {
        unchecked
        {
            x ^= x >> 16;
            x *= 0x7feb352d;
            x ^= x >> 15;
            x *= 0x846ca68b;
            x ^= x >> 16;
            return x;
        }
    }
}
