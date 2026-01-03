using BeginnersLuck.WorldGen.Data;
using BeginnersLuck.WorldGen.Local;

namespace BeginnersLuck.Game.World;

public static class WorldTilePalette
{
    // World TileId -> tileset index (pure visuals)
    public static int ToTileIndex(TileId id) => id switch
    {
        TileId.DeepWater     => 1,
        TileId.Ocean         => 1,
        TileId.ShallowWater  => 2,
        TileId.Coast         => 2,

        TileId.Sand          => 3,
        TileId.Grass         => 4,
        TileId.Dirt          => 5,
        TileId.Rock          => 6,
        TileId.Snow          => 7,
        TileId.Swamp         => 8,

        // If you don’t have distinct art yet, still keep distinct ids here.
        TileId.Hill          => 6,
        TileId.Mountain => 6,

        _ => 0
    };

    // World collision MUST be based on TileId (gameplay), not tileIndex (art)
    public static bool IsSolid(TileId id) => id switch
    {
        TileId.DeepWater => true,
        TileId.ShallowWater => true,
        TileId.Ocean => true,
        TileId.Coast => true,   // ✅ THIS IS THE FIX
        TileId.Mountain => true,
        // Optional, but recommended until passes exist:
        // TileId.Hill => true,
        _ => false
    };


    // Purpose based on flags (unchanged)
    public static LocalMapPurpose PurposeFromFlags(ushort flagsU16)
    {
        var flags = (TileFlags)flagsU16;
        if ((flags & TileFlags.Town) != 0) return LocalMapPurpose.Town;
        return LocalMapPurpose.Wilderness;
    }
}
