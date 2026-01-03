using BeginnersLuck.WorldGen.Data;

namespace BeginnersLuck.Game.World;

public static class LocalTilePalette
{
    // Tile indices based on tiles.png layout
    private const int GRASS = 0;
    private const int DIRT  = 1;
    private const int WATER = 2;
    private const int ROCK  = 3;

    public static int ToTileIndex(TileId id) => id switch
    {
        TileId.Grass        => GRASS,
        TileId.Dirt         => DIRT,
        TileId.Sand         => DIRT,
        TileId.Swamp        => DIRT,

        TileId.ShallowWater => WATER,
        TileId.DeepWater    => WATER,
        TileId.Ocean        => WATER,
        TileId.Coast        => WATER,

        TileId.Rock         => ROCK,
        TileId.Hill         => ROCK,
        TileId.Mountain     => ROCK,
        TileId.Snow         => ROCK,

        _ => GRASS
    };
}
