using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace BeginnersLuck.Game.State;

public sealed class WorldState
{
    public int WorldSeed { get; set; } = 777;

    // Local enter/exit pipeline state
    public WorldTravel Travel { get; } = new();

    // Persistent towns keyed by world tile position
    public Dictionary<Point, TownState> Towns { get; } = new();

    public TownState GetTown(Point worldTile)
    {
        if (!Towns.TryGetValue(worldTile, out var t))
        {
            // Stable per-tile seed (simple, deterministic, cheap)
            int seed = Hash(WorldSeed, worldTile.X, worldTile.Y);
            t = new TownState(seed);
            Towns[worldTile] = t;
        }

        return t;
    }

    private static int Hash(int a, int b, int c)
    {
        unchecked
        {
            int h = 17;
            h = h * 31 + a;
            h = h * 31 + b;
            h = h * 31 + c;
            return h;
        }
    }

    // Later:
    // public string SaveId { get; set; } = "dev";
    // public int WorldSize { get; set; } = 512;
    // public int ChunkSize { get; set; } = 32;
}
