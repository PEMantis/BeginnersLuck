using System;
using System.IO;

namespace BeginnersLuck.Game.World;

public static class WorldPaths
{
    // Single source of truth: repo root is "the nearest parent that contains Worlds/"
    public static string FindRepoRootWithWorlds()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "Worlds")))
                return dir.FullName;

            dir = dir.Parent;
        }

        // fallback: current working dir
        var cwd = Directory.GetCurrentDirectory();
        if (Directory.Exists(Path.Combine(cwd, "Worlds")))
            return cwd;

        throw new DirectoryNotFoundException("Could not locate repo root containing 'Worlds' directory.");
    }

    public static string WorldsRoot()
        => Path.Combine(FindRepoRootWithWorlds(), "Worlds");

    public static string SeedRoot(int seed)
        => Path.Combine(WorldsRoot(), $"seed{seed}");

    public static string WorldJsonPath(int seed)
        => Path.Combine(SeedRoot(seed), "world.json");

    public static string LocalDir(int seed, int wx, int wy)
        => Path.Combine(SeedRoot(seed), $"local_{wx}_{wy}");

    public static string LocalBin(int seed, int wx, int wy)
        => Path.Combine(LocalDir(seed, wx, wy), "local.mapbin");

    public static string LocalMeta(int seed, int wx, int wy)
        => Path.Combine(LocalDir(seed, wx, wy), "local.meta.json");
}
