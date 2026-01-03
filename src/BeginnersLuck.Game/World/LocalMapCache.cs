using System.IO;
using BeginnersLuck.WorldGen;
using BeginnersLuck.WorldGen.Local;
using BeginnersLuck.WorldGen.Local.Export;

namespace BeginnersLuck.Game.World;

public static class LocalMapCache
{
    public static string EnsureLocalExists(WorldMap world, int worldSeed, int wx, int wy, int localSize, LocalMapPurpose purpose)
    {
        string outDir = WorldPaths.LocalDir(worldSeed, wx, wy);
        string binPath = WorldPaths.LocalBin(worldSeed, wx, wy);

        if (File.Exists(binPath))
            return binPath;

        Directory.CreateDirectory(outDir);

        var req = new LocalMapRequest(
            WorldX: wx,
            WorldY: wy,
            Size: localSize,
            Seed: worldSeed,      // generator derives local seed internally
            Purpose: purpose
        );

        var gen = new LocalMapGenerator();
        var ctx = gen.GenerateWithContext(world, req);

        LocalMapExport.Write(outDir, ctx);

        return binPath;
    }
}
