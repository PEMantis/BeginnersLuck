using BeginnersLuck.WorldGen.Data;
using BeginnersLuck.WorldGen.Local;

namespace BeginnersLuck.Engine.World;

public sealed class LocalMapData
{
    public int Size { get; }
    public int Seed { get; }
    public int WorldX { get; }
    public int WorldY { get; }

    public LocalMapPurpose Purpose { get; }
    public BiomeId Biome { get; }

    public byte[] Elevation { get; }
    public byte[] Moisture { get; }
    public byte[] Temperature { get; }

    public TileId[] Terrain { get; }
    public TileFlags[] Flags { get; }

    public EdgePortals Portals { get; }
    public (int X, int Y)? TownCenter { get; }

    public LocalMapData(
        int size, int seed, int worldX, int worldY,
        LocalMapPurpose purpose, BiomeId biome,
        byte[] elevation, byte[] moisture, byte[] temperature,
        TileId[] terrain, TileFlags[] flags,
        EdgePortals portals, (int X, int Y)? townCenter)
    {
        Size = size;
        Seed = seed;
        WorldX = worldX;
        WorldY = worldY;

        Purpose = purpose;
        Biome = biome;

        Elevation = elevation;
        Moisture = moisture;
        Temperature = temperature;

        Terrain = terrain;
        Flags = flags;

        Portals = portals;
        TownCenter = townCenter;
    }

    public int Index(int x, int y) => (y * Size) + x;
}
