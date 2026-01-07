using System;
using BeginnersLuck.Game.Stats;

namespace BeginnersLuck.Game.Skills;

public sealed class SkillSystem
{
    private readonly SkillDb _skills;

    public SkillSystem(SkillDb skills)
    {
        _skills = skills ?? throw new ArgumentNullException(nameof(skills));
    }

    // Minimal “actor view” so we don’t force BattleScene changes yet.
    public SkillUseReport UseDamage(
        string skillId,
        string attackerName,
        StatBlock attackerStats,
        string targetName,
        StatBlock targetStats,
        Func<int> getTargetHp,
        Action<int> setTargetHp,
        Random rng)
    {
        var skill = _skills.Get(skillId);

        // MP cost enforcement will plug in once ActorState is in battle.
        int dmg = ComputeDamage(skill.FormulaId, attackerStats, targetStats, rng);

        int oldHp = getTargetHp();
        int newHp = Math.Max(0, oldHp - dmg);
        setTargetHp(newHp);

        return new SkillUseReport
        {
            SkillId = skill.Id,
            AttackerName = attackerName,
            TargetName = targetName,
            Amount = dmg,
            TargetDowned = (oldHp > 0 && newHp == 0)
        };
    }

    private static int ComputeDamage(string formulaId, StatBlock atk, StatBlock def, Random rng)
    {
        int A = Math.Max(0, atk[StatType.Atk]);
        int D = Math.Max(0, def[StatType.Def]);

        // V1 formulas (expand later)
        return formulaId switch
        {
            "atk_basic" => Math.Max(1, (A - (D / 2)) + rng.Next(-1, 2)),
            _ => Math.Max(1, (A - (D / 2)) + rng.Next(-1, 2))
        };
    }
}
