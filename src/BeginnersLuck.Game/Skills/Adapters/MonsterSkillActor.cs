using System;
using BeginnersLuck.Game.Monsters;
using BeginnersLuck.Game.Stats;
using BeginnersLuck.Game.Status;

namespace BeginnersLuck.Game.Skills.Adapters;

public sealed class MonsterSkillActor : ISkillActor
{
    private readonly MonsterInstance _m;
    public StatusController Statuses { get; }

    public MonsterSkillActor(MonsterInstance m, StatusController statuses)
    {
        _m = m ?? throw new ArgumentNullException(nameof(m));
        Statuses = statuses ?? throw new ArgumentNullException(nameof(statuses));
    }

    public string DebugName => _m.Def.Name;
    public int Level => 1;

    public int Hp => _m.Hp;
    public int HpMax => _m.Def.Stats[StatType.MaxHp];
    public int Mp => _m.Mp;
    public int MpMax => _m.Def.Stats[StatType.MaxMp];

    public int GetStat(StatId stat)
    {
        // Monsters are simple: take Def.Stats + status flat mods
        int Base(StatType s) => _m.Def.Stats[s] + Statuses.GetFlatMod(s);

        return stat switch
        {
            StatId.Atk => Base(StatType.Atk),
            StatId.Def => Base(StatType.Def),
            StatId.Mag => Base(StatType.Mag),
            StatId.Res => Base(StatType.Res),
            StatId.Spd => Base(StatType.Spd),
            StatId.HpMax => HpMax,
            StatId.MpMax => MpMax,
            _ => 0
        };
    }

    public void SpendHp(int amount) { if (amount > 0) _m.Damage(amount); }
    public void SpendMp(int amount) { if (amount > 0) { /* no-op for now */ } }

    public void TakeDamage(int amount) => _m.Damage(amount);
    public void Heal(int amount) => _m.Heal(amount);

    public void ApplyTimedStatMod(StatId stat, int delta, int durationTurns)
    {
        if (durationTurns <= 0 || delta == 0) return;

        var st = stat switch
        {
            StatId.Atk => StatType.Atk,
            StatId.Def => StatType.Def,
            StatId.Mag => StatType.Mag,
            StatId.Res => StatType.Res,
            StatId.Spd => StatType.Spd,
            _ => (StatType?)null
        };

        if (st == null) return;

        var mods = StatModifier.FromSingle(st.Value, delta);
        var statusId = $"{st.Value}_mod";
        Statuses.AddOrRefresh(statusId, mods, durationTurns, sourceTag: "skill");
    }
}
