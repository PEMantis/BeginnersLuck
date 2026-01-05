using System;

namespace BeginnersLuck.Game.Encounters;

public readonly record struct EnemyDef(string Id, string Name, int Hp);

public sealed class EncounterDef
{
    public string Id { get; }
    public string Name { get; }
    public EnemyDef[] Enemies { get; }

    public RewardRange Xp { get; init; } = new(3, 7);
    public RewardRange Gold { get; init; } = new(1, 4);

    public LootDrop[] Loot { get; init; } = Array.Empty<LootDrop>();

    public EncounterDef(string id, string name, EnemyDef[] enemies)
    {
        Id = id;
        Name = name;
        Enemies = enemies;

        // IMPORTANT: ItemId values must match ItemDb ids exactly.
        Loot = new[]
        {
            new LootDrop("herb", 65, 1, 2),
            new LootDrop("slime_gel", 25, 1, 1),
            new LootDrop("ring_tin", 8, 1, 1),
        };
    }
}
