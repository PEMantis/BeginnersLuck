using BeginnersLuck.WorldGen.Data;
using BeginnersLuck.WorldGen.Local;

namespace BeginnersLuck.Game.World;

public static class WorldTilePalette
{
    // map TileId -> tilesheet index (same tileset as local for now)
    public static int ToTileIndex(TileId id) => id switch
    {
        TileId.DeepWater     => 1,
        TileId.ShallowWater  => 2,
        TileId.Ocean         => 1,
        TileId.Coast         => 2,

        TileId.Sand          => 3,
        TileId.Grass         => 4,
        TileId.Dirt          => 5,
        TileId.Rock          => 6,
        TileId.Snow          => 7,
        TileId.Swamp         => 8,

        TileId.Hill          => 6,
        TileId.Mountain      => 6,

        _ => 0
    };

    public static bool IsSolid(TileId id) => id switch
    {
        TileId.DeepWater => true,
        TileId.ShallowWater => true,
        TileId.Ocean => true,
        TileId.Mountain => true,
        _ => false
    };

    public static LocalMapPurpose PurposeFromFlags(int flags)
        => ((TileFlags)flags & TileFlags.Town) != 0
            ? LocalMapPurpose.Town
            : LocalMapPurpose.Wilderness;
}
