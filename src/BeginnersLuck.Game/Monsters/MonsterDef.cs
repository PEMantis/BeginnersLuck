using System;
using BeginnersLuck.Game.Actors;

namespace BeginnersLuck.Game.Monsters;

public sealed class MonsterDef
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";

    public StatBlock Stats { get; init; } = new();

    // Skills/actions the monster can use in battle
    public string[] Skills { get; init; } = Array.Empty<string>();

    // Visual hook for later (battle sprites)
    public string SpriteKey { get; init; } = "";

    // Rewards/loot hooks (you can wire to Encounter rewards or to a LootDb later)
    public string LootTableId { get; init; } = "";

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Id)) throw new InvalidOperationException("MonsterDef.Id is required.");
        if (string.IsNullOrWhiteSpace(Name)) throw new InvalidOperationException($"MonsterDef '{Id}' has no Name.");
        if (Stats[StatType.MaxHp] <= 0) throw new InvalidOperationException($"MonsterDef '{Id}' MaxHp must be > 0.");
    }
}
