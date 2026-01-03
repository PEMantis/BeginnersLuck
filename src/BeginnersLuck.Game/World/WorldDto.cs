using System;
using System.Text.Json.Serialization;

namespace BeginnersLuck.Game.World;

// Matches your WorldGen's WorldMap JSON output (chunked)
public sealed class WorldDto
{
    public int Width { get; set; }
    public int Height { get; set; }
    public int ChunkSize { get; set; }
    public int Seed { get; set; }
    public int GeneratorVersion { get; set; }

    // IMPORTANT: chunked payload
    public ChunkDto[] Chunks { get; set; } = Array.Empty<ChunkDto>();
}

public sealed class ChunkDto
{
    public int Cx { get; set; }
    public int Cy { get; set; }

    // Per-cell arrays for this chunk (length = ChunkSize*ChunkSize)
    public int[] Terrain { get; set; } = Array.Empty<int>();
    public int[] Biome { get; set; } = Array.Empty<int>();
    public int[] Region { get; set; } = Array.Empty<int>();
    public int[] SubRegion { get; set; } = Array.Empty<int>();
    public int[] Flags { get; set; } = Array.Empty<int>();
}
