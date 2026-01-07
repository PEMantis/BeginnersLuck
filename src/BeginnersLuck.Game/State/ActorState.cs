using System;
using BeginnersLuck.Game.Stats;

namespace BeginnersLuck.Game.State;
public sealed class ActorState
{
    // Progression
    public int Level { get; set; } = 1;
    public int XpIntoLevel { get; set; }
    public int TotalXp { get; set; }

    // Resources
    public int Hp { get; set; }
    public int Mp { get; set; }

    // Stats
    public StatBlock BaseStats { get; } = new();
    public StatBlock DerivedStats { get; } = new();

    // Loadout
    public InventoryState Inventory { get; } = new();
    public string[] Skills { get; set; } = Array.Empty<string>();

    public int MaxHp => Math.Max(1, DerivedStats[StatType.MaxHp]);
    public int MaxMp => Math.Max(0, DerivedStats[StatType.MaxMp]);

    public bool Alive => Hp > 0;
}
