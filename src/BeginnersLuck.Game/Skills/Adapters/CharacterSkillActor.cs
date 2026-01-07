using System;
using BeginnersLuck.Game.State;
using BeginnersLuck.Game.Stats;
using BeginnersLuck.Game.Status;

namespace BeginnersLuck.Game.Skills.Adapters;

public sealed class CharacterSkillActor : ISkillActor
{
    private readonly CharacterState _c;
    private readonly StatsCalculator _stats;
    public StatusController Statuses { get; }

    public CharacterSkillActor(CharacterState c, StatsCalculator stats, StatusController statuses)
    {
        _c = c ?? throw new ArgumentNullException(nameof(c));
        _stats = stats ?? throw new ArgumentNullException(nameof(stats));
        Statuses = statuses ?? throw new ArgumentNullException(nameof(statuses));
    }

    public string DebugName => _c.Name;
    public int Level => _c.Level;

    public int Hp => _c.Hp;
    public int HpMax => _c.MaxHp;
    public int Mp => _c.Mp;
    public int MpMax => _c.MaxMp;

    public int GetStat(StatId stat)
    {
        var baseStats = _stats.ComputeFor(_c);
        var derived = _stats.ComputeDerived(baseStats, Statuses);

        return stat switch
        {
            StatId.Atk => derived[StatType.Atk],
            StatId.Def => derived[StatType.Def],
            StatId.Mag => derived[StatType.Mag],
            StatId.Res => derived[StatType.Res],
            StatId.Spd => derived[StatType.Spd],
            StatId.HpMax => _c.MaxHp,
            StatId.MpMax => _c.MaxMp,
            _ => 0
        };
    }

    public void SpendHp(int amount) { if (amount > 0) _c.Damage(amount); }

    // MP spending will come once MP rules are finalized; keeping interface consistent.
    public void SpendMp(int amount)
    {
        // CharacterState has Mp but no Spend method yet :contentReference[oaicite:5]{index=5}
        // Add later when you start using MP costs for real.
        if (amount <= 0) return;
        // (No-op for now)
    }

    public void TakeDamage(int amount) => _c.Damage(amount);
    public void Heal(int amount) => _c.Heal(amount);

    public void ApplyTimedStatMod(StatId stat, int delta, int durationTurns)
    {
        if (durationTurns <= 0 || delta == 0) return;

        // Map Skill StatId -> StatType
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
        var statusId = $"{st.Value}_mod"; // V1: deterministic id for refresh behavior
        Statuses.AddOrRefresh(statusId, mods, durationTurns, sourceTag: "skill");
    }
}
