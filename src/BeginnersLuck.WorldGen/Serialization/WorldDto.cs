using BeginnersLuck.WorldGen.Data;

namespace BeginnersLuck.WorldGen.Serialization;

public sealed class WorldDto
{
    public int Width { get; set; }
    public int Height { get; set; }
    public int ChunkSize { get; set; }
    public int Seed { get; set; }
    public int GeneratorVersion { get; set; }

    public List<ChunkDto> Chunks { get; set; } = new();
}

public sealed class ChunkDto
{
    public int Cx { get; set; }
    public int Cy { get; set; }

    public byte[] Elevation { get; set; } = Array.Empty<byte>();
    public byte[] Moisture { get; set; } = Array.Empty<byte>();
    public byte[] Temperature { get; set; } = Array.Empty<byte>();

    public byte[] Terrain { get; set; } = Array.Empty<byte>();
    public ushort[] Flags { get; set; } = Array.Empty<ushort>();
    public byte[] Biome { get; set; } = Array.Empty<byte>();

    public ushort[] Region { get; set; } = Array.Empty<ushort>();
    public ushort[] SubRegion { get; set; } = Array.Empty<ushort>();
}
