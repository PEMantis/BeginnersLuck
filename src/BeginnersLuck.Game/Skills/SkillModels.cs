using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BeginnersLuck.Game.Skills;

public enum SkillTarget
{
    Self,
    SingleEnemy,
    AllEnemies,
    SingleAlly,
    AllAllies
}

public enum SkillResource
{
    None,
    MP,
    HP
}

public enum SkillEffectType
{
    Damage,
    Heal,
    BuffStat,   // temporary: +Stat for DurationTurns
    DebuffStat  // temporary: -Stat for DurationTurns
}

public enum StatId
{
    HpMax,
    MpMax,
    Atk,
    Def,
    Mag,
    Res,
    Spd
}

public sealed class SkillCostDef
{
    public SkillResource Resource { get; set; } = SkillResource.MP;
    public int Amount { get; set; } = 0;
}

public sealed class SkillEffectDef
{
    public SkillEffectType Type { get; set; }

    // For Damage/Heal: "Power" is the base magnitude added to scaling.
    // For Buff/Debuff: Amount is applied to Stat for DurationTurns.
    public int Power { get; set; } = 0;

    // Optional stat for buff/debuff
    public StatId? Stat { get; set; }

    public int Amount { get; set; } = 0;
    public int DurationTurns { get; set; } = 0;

    // Scaling knobs (simple and JRPG-ish)
    public float UserAtkScale { get; set; } = 1.0f;   // Damage: user ATK contributes
    public float UserMagScale { get; set; } = 0.0f;   // Damage/Heal: user MAG contributes
    public float TargetDefScale { get; set; } = 0.5f; // Damage: target DEF mitigates
    public float TargetResScale { get; set; } = 0.0f; // Damage: target RES mitigates
}

public sealed class SkillDef
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";

    public SkillTarget Target { get; set; } = SkillTarget.SingleEnemy;

    public SkillCostDef Cost { get; set; } = new();

    public List<string> Tags { get; set; } = new();

    public List<SkillEffectDef> Effects { get; set; } = new();
}
