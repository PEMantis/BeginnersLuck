using System;
using BeginnersLuck.Game.Monsters;
using BeginnersLuck.Game.Stats;

namespace BeginnersLuck.Game.Actors;

public sealed class BattleEnemy
{
    public string MonsterId { get; }
    public string Name { get; }
    public int Hp { get; private set; }
    public int MaxHp { get; }
    public bool Alive => Hp > 0;

    public string SpriteKey { get; }

    public BattleEnemy(MonsterDef def)
    {
        MonsterId = def.Id;
        Name = def.Name;

        MaxHp = Math.Max(1, def.Stats[StatType.MaxHp]);
        Hp = MaxHp;

        SpriteKey = def.SpriteKey ?? "";
    }

    public void Damage(int amount)
    {
        if (amount <= 0) return;
        Hp = Math.Max(0, Hp - amount);
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;
        Hp = Math.Min(MaxHp, Hp + amount);
    }
}
