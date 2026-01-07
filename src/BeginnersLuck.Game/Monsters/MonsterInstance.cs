using System;
using BeginnersLuck.Game.Actors;
using BeginnersLuck.Game.Stats;

namespace BeginnersLuck.Game.Monsters;

public sealed class MonsterInstance
{
    public MonsterDef Def { get; }
    public int Hp { get; private set; }
    public int Mp { get; private set; }

    public bool Alive => Hp > 0;

    public MonsterInstance(MonsterDef def)
    {
        Def = def ?? throw new ArgumentNullException(nameof(def));
        Hp = def.Stats[StatType.MaxHp];
        Mp = def.Stats[StatType.MaxMp];
    }

    public void Damage(int amount)
    {
        if (amount <= 0) return;
        Hp = Math.Max(0, Hp - amount);
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;
        int max = Def.Stats[StatType.MaxHp];
        Hp = Math.Min(max, Hp + amount);
    }
}
