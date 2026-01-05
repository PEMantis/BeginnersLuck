using BeginnersLuck.WorldGen.Data;

namespace BeginnersLuck.WorldGen.Local;

public enum LocalMapPurpose : byte
{
    None = 0,
    Wilderness = 1,
    Town = 2,
    Road = 3,
    Ruins = 4,
}

public sealed record LocalMapRequest(
    int WorldX,
    int WorldY,
    int Size,
    int Seed,
    LocalMapPurpose Purpose
);
