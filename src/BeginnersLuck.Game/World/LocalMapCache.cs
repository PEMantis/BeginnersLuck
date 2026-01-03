using System;
using System.IO;
using BeginnersLuck.Engine.World;
using BeginnersLuck.WorldGen;
using BeginnersLuck.WorldGen.Data;
using BeginnersLuck.WorldGen.Local;

namespace BeginnersLuck.Game.World;

public static class LocalMapCache
{
    private static readonly LocalMapGenerator _gen = new();

    /// <summary>
    /// Ensures local map exists AND is playable.
    /// If existing bin is unplayable, deletes and regenerates.
    /// Tries several derived seeds to avoid pathological generations.
    /// </summary>
    public static string EnsureLocalExists(
        WorldMap world,
        int worldSeed,
        int wx,
        int wy,
        int localSize,
        LocalMapPurpose purpose)
    {
        string path = WorldPaths.LocalBin(worldSeed, wx, wy);

        // 1) If it exists, validate it. If playable, use it.
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

        // 2) Regenerate with multiple attempts using derived seeds
        const int maxAttempts = 12;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            int attemptSeed = DeriveAttemptSeed(worldSeed, wx, wy, (uint)attempt);

            // IMPORTANT:
            // LocalMapGenerator will internally derive a local seed from request.Seed + wx/wy.
            // We vary request.Seed per attempt to get different deterministic outcomes.
            var req = new LocalMapRequest(
                Seed: attemptSeed,
                WorldX: wx,
                WorldY: wy,
                Size: localSize,
                Purpose: purpose
            );

            // Generate WITH context so we can pull biome/portals/town center if your steps populate them.
            var ctx = _gen.GenerateWithContext(world, req);

            var data = LocalMapAdapter.ToData(
                map: ctx.Map,
                purpose: purpose,
                biome: ctx.Biome,
                portals: ctx.Portals,
                townCenter: null
            );

            // Validate BEFORE saving
            var rep = LocalMapValidator.Validate(data, purpose);
            Console.WriteLine($"[LocalMapCache] Gen attempt {attempt} seed={attemptSeed} => playable={rep.Playable} largest={rep.LargestRegion} edge={rep.LargestTouchesEdge} roads={rep.RoadCount} reason={rep.Reason}");

            if (!rep.Playable)
                continue;

            // Save only playable maps
            LocalMapBinWriter.Save(path, data);
            return path;
        }

        // Last resort: generate once and save, but warn loudly.
        Console.WriteLine($"[LocalMapCache] WARNING: Could not generate playable map for ({wx},{wy}) after {maxAttempts} attempts. Saving last attempt anyway: {path}");

        int fallbackSeed = DeriveAttemptSeed(worldSeed, wx, wy, 999);
        var fallbackReq = new LocalMapRequest(
            Seed: fallbackSeed,
            WorldX: wx,
            WorldY: wy,
            Size: localSize,
            Purpose: purpose
        );

        var fallbackCtx = _gen.GenerateWithContext(world, fallbackReq);
        var fallbackData = LocalMapAdapter.ToData(fallbackCtx.Map, purpose, fallbackCtx.Biome, fallbackCtx.Portals, null);
        LocalMapBinWriter.Save(path, fallbackData);
        return path;
    }

    private static void TryDelete(string path)
    {
        try { File.Delete(path); }
        catch (Exception e) { Console.WriteLine($"[LocalMapCache] Failed to delete {path}: {e.Message}"); }
    }

    private static int DeriveAttemptSeed(int worldSeed, int wx, int wy, uint attempt)
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
