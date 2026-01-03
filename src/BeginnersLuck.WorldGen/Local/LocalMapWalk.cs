using BeginnersLuck.WorldGen.Data;

namespace BeginnersLuck.WorldGen.Local;

public static class LocalMapWalk
{
    // Adjust this based on your TileId set.
    public static bool IsWalkable(LocalMap m, int x, int y)
    {
        if ((uint)x >= (uint)m.Size || (uint)y >= (uint)m.Size) return false;

        var id = m.Terrain[m.Index(x, y)];

        return id switch
        {
            TileId.Grass => true,
            TileId.Dirt => true,
            TileId.Sand => true,
            TileId.Rock => true,
            TileId.Snow => true,
            TileId.Swamp => true,
            TileId.Hill => true,

            // Blocked
            TileId.Unknown => false,
            TileId.DeepWater => false,
            TileId.ShallowWater => false,
            TileId.Ocean => false,
            TileId.Coast => false,   // recommend false initially
            TileId.Mountain => false,

            _ => false
        };
    }


    public static bool HasFlag(LocalMap m, int x, int y, TileFlags f)
        => ((m.Flags[m.Index(x, y)] & f) != 0);

    public static float MoveCost(LocalMap m, int x, int y)
    {
        var id = m.Terrain[m.Index(x, y)];
        return id switch
        {
            TileId.Swamp => 1.6f,
            TileId.Sand => 1.2f,
            TileId.Snow => 1.3f,
            TileId.Hill => 1.4f,
            _ => 1.0f
        };
    }

}
