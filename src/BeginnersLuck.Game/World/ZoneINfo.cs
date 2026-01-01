namespace BeginnersLuck.Game.World;

public readonly record struct ZoneInfo(
    ZoneId Id,
    int Danger,                 // 0..9
    string EncounterTableId     // "none", "plains_low", etc (we’ll use this later)
);
