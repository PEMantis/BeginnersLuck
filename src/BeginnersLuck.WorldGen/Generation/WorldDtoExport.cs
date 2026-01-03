namespace BeginnersLuck.WorldGen.Export;

public sealed class WorldExportDto
{
    public int Width { get; set; }
    public int Height { get; set; }
    public int ChunkSize { get; set; }
    public int Seed { get; set; }
    public int GeneratorVersion { get; set; }

    public List<ChunkExportDto> Chunks { get; set; } = new();
}

public sealed class ChunkExportDto
{
    public int Cx { get; set; }
    public int Cy { get; set; }

    public byte[] Terrain { get; set; } = Array.Empty<byte>();
    public byte[] Biome { get; set; } = Array.Empty<byte>();

    public ushort[] Region { get; set; } = Array.Empty<ushort>();
    public ushort[] SubRegion { get; set; } = Array.Empty<ushort>();

    public ushort[] Flags { get; set; } = Array.Empty<ushort>();
}
