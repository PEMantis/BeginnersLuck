namespace BeginnersLuck.WorldGen.Data;

public sealed class Chunk
{
    public int ChunkSize { get; }
    public int Cx { get; }
    public int Cy { get; }

    public byte[] Elevation { get; }
    public byte[] Moisture { get; }
    public byte[] Temperature { get; }

    public TileId[] Terrain { get; }
    public TileFlags[] Flags { get; }

    public BiomeId[] Biome { get; }
    public ushort[] Region { get; }
    public ushort[] SubRegion { get; }

    public Chunk(int chunkSize, int cx, int cy)
    {
        ChunkSize = chunkSize;
        Cx = cx;
        Cy = cy;

        int n = chunkSize * chunkSize;
        Elevation = new byte[n];
        Moisture = new byte[n];
        Temperature = new byte[n];

        Terrain = new TileId[n];
        Flags = new TileFlags[n];
        Biome = new BiomeId[n];
        Region = new ushort[n];
        SubRegion = new ushort[n];
    }

    public int Index(int lx, int ly) => (ly * ChunkSize) + lx;
}
