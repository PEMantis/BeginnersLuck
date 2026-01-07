using System;

namespace BeginnersLuck.Game.Skills;

public sealed class SkillDef
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";

    public SkillTarget Target { get; init; } = SkillTarget.EnemySingle;
    public int MpCost { get; init; } = 0;

    public SkillEffect Effect { get; init; } = SkillEffect.Damage;

    // "atk_basic", "firebolt", etc. Code-backed for now.
    public string FormulaId { get; init; } = "atk_basic";

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Id)) throw new InvalidOperationException("SkillDef.Id is required.");
        if (string.IsNullOrWhiteSpace(Name)) throw new InvalidOperationException($"SkillDef '{Id}' has no Name.");
        if (MpCost < 0) throw new InvalidOperationException($"SkillDef '{Id}' MpCost must be >= 0.");
        if (string.IsNullOrWhiteSpace(FormulaId)) throw new InvalidOperationException($"SkillDef '{Id}' FormulaId is required.");
    }
}
