using BeginnersLuck.WorldGen.Data;

namespace BeginnersLuck.Game.World;

public static class LocalTilePalette
{
    // WorldGen TileId -> tileset tile index
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

        TileId.Hill          => 6,  // temp: rock
        TileId.Mountain      => 6,  // temp: rock

        _ => 0
    };

    // Gameplay solidity in WorldGen terms
    public static bool IsSolid(TileId id) => id switch
    {
        TileId.DeepWater => true,
        TileId.ShallowWater => true,
        TileId.Ocean => true,
        TileId.Mountain => true,
        _ => false
    };

    // IMPORTANT: solidity in TileMap space (tileIndex)
    public static bool IsSolidTileIndex(int tileIndex) => tileIndex switch
    {
        1 => true, // DeepWater/Ocean
        2 => true, // ShallowWater/Coast (for now)
        _ => false
    };
}
