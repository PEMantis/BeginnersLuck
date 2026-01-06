using BeginnersLuck.WorldGen.Data;

namespace BeginnersLuck.Game.World;

public static class WorldCollisionResolver
{
    public static WorldCollision Resolve(TileId tid, TileFlags flags)
    {
        // Flags that hard-block regardless of base tile
        if ((flags & TileFlags.Cliff) != 0) return WorldCollision.Hard;
        if ((flags & TileFlags.Coast) != 0) return WorldCollision.Hard;

        // Base terrain blocking
        if (WorldTilePalette.IsSolid(tid)) return WorldCollision.Hard;

        // Ruins and Roads are walkable (for now)
        return WorldCollision.None;
    }

    public static bool IsSolid(TileId tid, TileFlags flags)
        => Resolve(tid, flags) == WorldCollision.Hard;
}
