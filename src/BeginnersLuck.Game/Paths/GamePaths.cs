using System;
using System.IO;

namespace BeginnersLuck.Game.Paths;

public static class GamePaths
{
    // Walk up until we find a folder containing "Worlds" (repo root in dev).
    public static string FindRepoRootWithWorlds()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir != null)
        {
            var worlds = Path.Combine(dir.FullName, "Worlds");
            if (Directory.Exists(worlds))
                return dir.FullName;

            dir = dir.Parent;
        }

        // Fallback: current working directory
        var cwd = Directory.GetCurrentDirectory();
        if (Directory.Exists(Path.Combine(cwd, "Worlds")))
            return cwd;

        throw new DirectoryNotFoundException("Could not locate repo root containing 'Worlds' directory.");
    }

    public static string WorldsDir()
        => Path.Combine(FindRepoRootWithWorlds(), "Worlds");
}
