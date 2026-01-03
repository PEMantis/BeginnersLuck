using BeginnersLuck.WorldGen.Data;

namespace BeginnersLuck.Game.World;

public static class LocalTilePalette
{
    // WorldGen TileId -> tileset tile index
    public static int ToTileIndex(TileId id) => id switch
    {
        TileId.DeepWater    => 1,
        TileId.Ocean        => 1,

        TileId.ShallowWater => 2,
        TileId.Coast        => 2,

        TileId.Sand         => 3,
        TileId.Grass        => 4,
        TileId.Dirt         => 5,

        TileId.Rock         => 6,
        TileId.Hill         => 6,
        TileId.Mountain     => 6,

        TileId.Snow         => 7,
        TileId.Swamp        => 8,

        _ => 0
    };

    // WorldGen gameplay truth (authoritative)
    public static bool IsSolid(TileId id) => id switch
    {
        TileId.DeepWater    => true,
        TileId.ShallowWater => true,
        TileId.Ocean        => true,
        TileId.Coast        => true,
        TileId.Mountain     => true,
        _ => false
    };
}
