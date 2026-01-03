using System;
using System.IO;
using BeginnersLuck.Engine.World;
using BeginnersLuck.WorldGen.Local;

namespace BeginnersLuck.WorldGen;

public static class PlayableLocalMapCache
{
    /// <summary>
    /// Calls your existing LocalMapCache.EnsureLocalExists(...) to get a bin path,
    /// then validates the loaded map. If unplayable, deletes it and retries with
    /// a derived seed (so the filename changes and you get a different generation).
    /// </summary>
    public static string EnsurePlayableLocalExists(
        WorldMap world,
        int worldSeed,
        int wx,
        int wy,
        int localSize,
        LocalMapPurpose purpose,
        int maxAttempts = 10)
    {
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            int attemptSeed = attempt == 0 ? worldSeed : DeriveSeed(worldSeed, wx, wy, attempt);

            // This is YOUR existing generator/cache pipeline.
            // No signature guessing, no writer needed.
            string path = LocalMapCache.EnsureLocalExists(
                world: world,
                worldSeed: attemptSeed,
                wx: wx,
                wy: wy,
                localSize: localSize,
                purpose: purpose);

            if (!File.Exists(path))
            {
                Console.WriteLine($"[PlayableLocalMapCache] Missing bin after EnsureLocalExists: {path}");
                continue;
            }

            var map = LocalMapBinLoader.Load(path);
            var rep = LocalMapValidator.Validate(map, purpose);

            Console.WriteLine(
                $"[PlayableLocalMapCache] ({wx},{wy}) attempt={attempt} seed={attemptSeed} " +
                $"playable={rep.Playable} largest={rep.LargestRegion} edge={rep.LargestTouchesEdge} " +
                $"walkable={rep.WalkableCount} roads={rep.RoadCount} reason={rep.Reason}");

            if (rep.Playable)
                return path;

            // Delete the bad bin so we don't keep reloading the same trap
            TryDelete(path);
        }

        // If we fail repeatedly, fall back to the baseline seed's map path (even if bad)
        // This preserves behavior but logs the issue.
        Console.WriteLine($"[PlayableLocalMapCache] WARNING: Could not find playable local after {maxAttempts} attempts for ({wx},{wy}). Falling back.");
        return LocalMapCache.EnsureLocalExists(world, worldSeed, wx, wy, localSize, purpose);
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception e)
        {
            Console.WriteLine($"[PlayableLocalMapCache] Failed to delete {path}: {e.Message}");
        }
    }

    private static int DeriveSeed(int worldSeed, int wx, int wy, int attempt)
    {
        unchecked
        {
            uint h = (uint)worldSeed;
            h = Mix(h ^ (uint)wx);
            h = Mix(h ^ (uint)wy);
            h = Mix(h ^ (uint)attempt);
            return (int)h;
        }
    }

    private static uint Mix(uint x)
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
