namespace BeginnersLuck.WorldGen;

public sealed record WorldGenRequest(
    int Width,
    int Height,
    int ChunkSize,
    int Seed,
    WorldGenSettings Settings);
