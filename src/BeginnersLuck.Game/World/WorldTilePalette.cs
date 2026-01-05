using BeginnersLuck.WorldGen.Data;
using BeginnersLuck.WorldGen.Local;

namespace BeginnersLuck.Game.World;

public static class WorldTilePalette
{
    /// <summary>
    /// Maps TileId → tileset index (tiles.png).
    /// Adjust indices to match your actual tileset layout.
    /// </summary>
    public static int ToTileIndex(TileId id) => id switch
    {
        TileId.DeepWater    => 0,
        TileId.Ocean        => 1,
        TileId.ShallowWater => 2,

        TileId.Sand         => 3,
        TileId.Coast        => 3, // reuse sand tile unless you have a coast tile

        TileId.Grass        => 4,
        TileId.Dirt         => 5,

        TileId.Forest       => 4, // forest uses grass ground; trees are overlays
        TileId.Swamp        => 8,

        TileId.Rock         => 6,
        TileId.Hill         => 12,
        TileId.Mountain     => 11,

        TileId.Snow         => 7,

        TileId.Ruins        => 4, // ruins sit on grass ground; pillars drawn separately

        _ => 4
    };

    /// <summary>
    /// Base terrain solidity on WORLD MAP.
    /// Flags (Cliff, Coast, Ruins POI) can add more blocking.
    /// </summary>
    public static bool IsSolid(TileId id) => id switch
    {
        TileId.DeepWater    => true,
        TileId.Ocean        => true,
        TileId.ShallowWater => true,

        TileId.Mountain     => true,
        TileId.Rock         => true,

        // Everything else is passable by default
        _ => false
    };

    /// <summary>
    /// Determines what kind of LOCAL MAP to generate
    /// based on world flags, not terrain.
    /// </summary>
    public static LocalMapPurpose PurposeFromFlags(ushort flagsU16)
    {
        var flags = (TileFlags)flagsU16;

        if ((flags & TileFlags.Town) != 0)
            return LocalMapPurpose.Town;

        if ((flags & TileFlags.Ruins) != 0)
            return LocalMapPurpose.Ruins;

        if ((flags & TileFlags.Road) != 0)
            return LocalMapPurpose.Road;

        return LocalMapPurpose.None;
    }

    public static bool IsLand(TileId id)
        => id is not (TileId.DeepWater or TileId.Ocean or TileId.ShallowWater);
}
