using System;
using System.IO;

namespace BeginnersLuck.Game.World;

public static class WorldPaths
{
    public static string FindRepoRootWithWorlds()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "Worlds")))
                return dir.FullName;

            dir = dir.Parent;
        }

        var cwd = Directory.GetCurrentDirectory();
        if (Directory.Exists(Path.Combine(cwd, "Worlds")))
            return cwd;

        throw new DirectoryNotFoundException("Could not locate repo root containing 'Worlds' directory.");
    }

    public static string WorldsDir() => Path.Combine(FindRepoRootWithWorlds(), "Worlds");

    public static string LocalMapBinPath(int worldSeed, int wx, int wy)
        => Path.Combine(WorldsDir(), $"seed{worldSeed}", $"local_{wx}_{wy}", "local.mapbin");
}
