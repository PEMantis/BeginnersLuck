using BeginnersLuck.WorldGen.Data;

namespace BeginnersLuck.WorldGen.Local;

public sealed class LocalMap
{
    public int Size { get; }
    public int Seed { get; }
    public int WorldX { get; }
    public int WorldY { get; }

    public byte[] Elevation { get; }      // 0..255
    public byte[] Moisture { get; }       // 0..255
    public byte[] Temperature { get; }    // 0..255

    public TileId[] Terrain { get; }
    public TileFlags[] Flags { get; }     // River/Road etc.

    public LocalMap(int size, int seed, int worldX, int worldY)
    {
        Size = size;
        Seed = seed;
        WorldX = worldX;
        WorldY = worldY;

        int n = size * size;
        Elevation = new byte[n];
        Moisture = new byte[n];
        Temperature = new byte[n];

        Terrain = new TileId[n];
        Flags = new TileFlags[n];
    }

    public int Index(int x, int y) => (y * Size) + x;
}
