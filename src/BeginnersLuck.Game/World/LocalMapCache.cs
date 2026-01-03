using System;
using System.IO;
using BeginnersLuck.Engine.World;
using BeginnersLuck.Game.World;
using BeginnersLuck.WorldGen.Local;

namespace BeginnersLuck.WorldGen;

public static class LocalMapCache
{
    public static string EnsureLocalExists(
        WorldMap world,
        int worldSeed,
        int wx,
        int wy,
        int localSize,
        LocalMapPurpose purpose)
    {
        string path = WorldPaths.LocalBin(worldSeed, wx, wy);

        // If it exists, validate; if playable, return.
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

        // Regenerate attempts
        const int maxAttempts = 12;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            int localSeed = DeriveLocalSeed(worldSeed, wx, wy, (uint)attempt);

            // Generate with compat (guaranteed playable-ish)
            var map = LocalMapGeneratorCompat.Generate(world, localSeed, wx, wy, localSize, purpose);

            // Save bin using writer that matches loader
            LocalMapBinWriter.Save(path, map);

            // Validate
            var rep = LocalMapValidator.Validate(map, purpose);
            Console.WriteLine($"[LocalMapCache] Gen attempt {attempt} seed={localSeed} => playable={rep.Playable} largest={rep.LargestRegion} edge={rep.LargestTouchesEdge} roads={rep.RoadCount} reason={rep.Reason}");

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
