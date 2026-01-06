namespace BeginnersLuck.Game.World;

public sealed class ZoneInfo
{
    public ZoneId Id { get; init; }
    public string EncounterTableId { get; init; } = "none";
    public float Danger { get; init; }

    // New
    public float EncounterChanceMultiplier { get; init; } = 1f; // 1 = normal
    public float MoveTimeMultiplier { get; init; } = 1f;        // 1 = normal

    public ZoneInfo(ZoneId id, float danger, string encounterTableId)
    {
        Id = id;
        Danger = danger;
        EncounterTableId = encounterTableId;
    }
}
