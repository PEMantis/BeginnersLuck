using System;

namespace BeginnersLuck.Game.Encounters;

public readonly record struct EnemyDef(string Id, string Name, int Hp);


public sealed class EncounterDef
{
    public string Id { get; }
    public string Name { get; }
    public EnemyDef[] Enemies { get; }

    // Rewards (new)
    public RewardRange Xp { get; init; } = new(3, 7);
    public RewardRange Gold { get; init; } = new(1, 4);

    // Loot (new)
    public LootDrop[] Loot { get; init; } = Array.Empty<LootDrop>();

    // ✅ Back-compat constructor (fixes your CS1729 errors)
    public EncounterDef(string id, string name, EnemyDef[] enemies)
    {
        Id = id;
        Name = name;
        Enemies = enemies;
        Loot = new[]
            {
                new LootDrop("gel", 65, 1, 2),
                new LootDrop("herb", 25, 1, 1),
                new LootDrop("ring_tin", 8, 1, 1),
            };
    }
}
