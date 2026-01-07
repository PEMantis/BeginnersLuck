using System;
using System.Collections.Generic;

namespace BeginnersLuck.Game.Skills;

public sealed class SkillSystem
{
    private readonly SkillDb _db;

    public SkillSystem(SkillDb db)
    {
        _db = db;
    }

    public SkillDef GetDef(string id) => _db.Get(id);

    public bool CanUse(ISkillActor user, string skillId, out string reason)
    {
        var def = _db.Get(skillId);

        var cost = def.Cost ?? new SkillCostDef();

        if (cost.Amount <= 0 || cost.Resource == SkillResource.None)
        {
            reason = "";
            return true;
        }

        switch (cost.Resource)
        {
            case SkillResource.MP:
                if (user.Mp < cost.Amount)
                {
                    reason = "Not enough MP.";
                    return false;
                }
                break;

            case SkillResource.HP:
                // Don’t allow self-KO via cost (common JRPG rule-of-thumb)
                if (user.Hp <= cost.Amount)
                {
                    reason = "Not enough HP.";
                    return false;
                }
                break;

            default:
                break;
        }

        reason = "";
        return true;
    }

    public SkillResolution Resolve(ISkillActor user, string skillId, IReadOnlyList<ISkillActor> targets)
    {
        var def = _db.Get(skillId);

        if (!CanUse(user, skillId, out var reason))
            throw new InvalidOperationException($"Cannot use skill '{skillId}': {reason}");

        SpendCost(user, def);

        var log = new List<SkillEvent>();

        foreach (var t in targets)
        {
            foreach (var fx in def.Effects)
            {
                ApplyEffect(user, t, fx, log);
            }
        }

        return new SkillResolution(def, log);
    }

    private static void SpendCost(ISkillActor user, SkillDef def)
    {
        var cost = def.Cost;
        if (cost == null || cost.Amount <= 0 || cost.Resource == SkillResource.None)
            return;

        if (cost.Resource == SkillResource.MP) user.SpendMp(cost.Amount);
        else if (cost.Resource == SkillResource.HP) user.SpendHp(cost.Amount);
    }

    private static void ApplyEffect(ISkillActor user, ISkillActor target, SkillEffectDef fx, List<SkillEvent> log)
    {
        switch (fx.Type)
        {
            case SkillEffectType.Damage:
            {
                var dmg = ComputeDamage(user, target, fx);
                target.TakeDamage(dmg);
                log.Add(SkillEvent.Damage(user.DebugName, target.DebugName, dmg));
                break;
            }

            case SkillEffectType.Heal:
            {
                var heal = ComputeHeal(user, target, fx);
                target.Heal(heal);
                log.Add(SkillEvent.Heal(user.DebugName, target.DebugName, heal));
                break;
            }

            case SkillEffectType.BuffStat:
            {
                if (fx.Stat == null || fx.DurationTurns <= 0 || fx.Amount == 0) break;
                target.ApplyTimedStatMod(fx.Stat.Value, Math.Abs(fx.Amount), fx.DurationTurns);
                log.Add(SkillEvent.StatMod(user.DebugName, target.DebugName, fx.Stat.Value, +Math.Abs(fx.Amount), fx.DurationTurns));
                break;
            }

            case SkillEffectType.DebuffStat:
            {
                if (fx.Stat == null || fx.DurationTurns <= 0 || fx.Amount == 0) break;
                target.ApplyTimedStatMod(fx.Stat.Value, -Math.Abs(fx.Amount), fx.DurationTurns);
                log.Add(SkillEvent.StatMod(user.DebugName, target.DebugName, fx.Stat.Value, -Math.Abs(fx.Amount), fx.DurationTurns));
                break;
            }

            default:
                break;
        }
    }

    private static int ComputeDamage(ISkillActor user, ISkillActor target, SkillEffectDef fx)
    {
        // A very “JRPG core” formula:
        // base = fx.Power + user(Atk/Mag scaling) - target(Def/Res scaling)
        // min 1
        var atk = user.GetStat(StatId.Atk);
        var mag = user.GetStat(StatId.Mag);
        var def = target.GetStat(StatId.Def);
        var res = target.GetStat(StatId.Res);

        var raw =
            fx.Power
            + atk * fx.UserAtkScale
            + mag * fx.UserMagScale
            - def * fx.TargetDefScale
            - res * fx.TargetResScale;

        var dmg = (int)MathF.Round(raw);
        if (dmg < 1) dmg = 1;
        return dmg;
    }

    private static int ComputeHeal(ISkillActor user, ISkillActor target, SkillEffectDef fx)
    {
        // Heal formula:
        // base = fx.Power + user(Mag scaling)
        var mag = user.GetStat(StatId.Mag);
        var raw = fx.Power + mag * fx.UserMagScale;
        var heal = (int)MathF.Round(raw);
        if (heal < 1) heal = 1;
        return heal;
    }
}

public sealed record SkillResolution(SkillDef Def, IReadOnlyList<SkillEvent> Events);

public sealed record SkillEvent(string Kind, string Source, string Target, int Amount, StatId? Stat, int DurationTurns)
{
    public static SkillEvent Damage(string src, string tgt, int amt)
        => new("Damage", src, tgt, amt, null, 0);

    public static SkillEvent Heal(string src, string tgt, int amt)
        => new("Heal", src, tgt, amt, null, 0);

    public static SkillEvent StatMod(string src, string tgt, StatId stat, int delta, int turns)
        => new("StatMod", src, tgt, delta, stat, turns);
}
