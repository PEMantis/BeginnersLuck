using BeginnersLuck.WorldGen.Data;

namespace BeginnersLuck.WorldGen.Local;

public enum LocalMapPurpose : byte
{
    Wilderness = 0,
    Town = 1
}

public sealed record LocalMapRequest(
    int WorldX,
    int WorldY,
    int Size,
    int Seed,
    LocalMapPurpose Purpose
);
