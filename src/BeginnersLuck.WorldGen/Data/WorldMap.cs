using BeginnersLuck.WorldGen.Data;

namespace BeginnersLuck.WorldGen;

public sealed class WorldMap
{
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required int ChunkSize { get; init; }
    public required int Seed { get; init; }
    public required int GeneratorVersion { get; init; }

    private readonly Dictionary<(int cx, int cy), Chunk> _chunks = new();

    public int ChunksX => Width / ChunkSize;
    public int ChunksY => Height / ChunkSize;

    public Chunk GetChunk(int cx, int cy) => _chunks[(cx, cy)];
    public void SetChunk(int cx, int cy, Chunk chunk) => _chunks[(cx, cy)] = chunk;

    public IEnumerable<(int cx, int cy)> AllChunkCoords()
    {
        for (int cy = 0; cy < ChunksY; cy++)
        for (int cx = 0; cx < ChunksX; cx++)
            yield return (cx, cy);
    }
}
