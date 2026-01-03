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

    public static string LocalMapBinPath(int seed, int wx, int wy)
    => Path.Combine(SeedRoot(seed), $"local_{wx}_{wy}", "local.mapbin");

    public static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        // AppContext.BaseDirectory points to bin/.../net9.0, so walk up.
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "BeginnersLuck.sln")))
                return dir.FullName;

            dir = dir.Parent;
        }

        // fallback: current working dir (still better than crashing)
        return Directory.GetCurrentDirectory();
    }

    public static string WorldsRoot()
        => Path.Combine(FindRepoRoot(), "Worlds");

    public static string SeedRoot(int seed)
        => Path.Combine(WorldsRoot(), $"seed{seed}");

    public static string LocalDir(int seed, int wx, int wy)
        => Path.Combine(SeedRoot(seed), $"local_{wx}_{wy}");

    public static string LocalBin(int seed, int wx, int wy)
        => Path.Combine(LocalDir(seed, wx, wy), "local.mapbin");
}
