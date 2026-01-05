using System;
using System.Reflection;
using BeginnersLuck.Game.Encounters;
using BeginnersLuck.Game.Services;

namespace BeginnersLuck.Game.World;

public sealed class WorldEncounterSystem
{
    // Tune these freely
    public float ChancePerStep { get; set; } = 0.12f; // 12% per successful move
    public int CooldownSteps { get; set; } = 3;       // min steps between encounters

    private int _cooldown;

    public void TickOnMove() 
    {
        if (_cooldown > 0) _cooldown--;
    }

    public bool TryRoll(GameServices s, out EncounterDef? encounter)
    {
        encounter = null;

        if (_cooldown > 0) return false;

        // Roll
        if (s.Rng.NextDouble() > ChancePerStep)
            return false;

        // Got a hit: attempt to fetch an encounter from EncounterDirector
        if (TryGetEncounterFromDirector(s, out encounter) && encounter != null)
        {
            _cooldown = CooldownSteps;
            return true;
        }

        // If we can’t resolve an encounter yet, still consume cooldown so it doesn't spam.
        _cooldown = CooldownSteps;
        return false;
    }

    private static bool TryGetEncounterFromDirector(GameServices s, out EncounterDef? encounter)
    {
        encounter = null;

        object director = s.EncounterDirector;
        var t = director.GetType();

        // Try common patterns in order. Add your real method here once you know its signature.
        // 1) bool TryGetRandomEncounter(out EncounterDef enc)
        if (TryInvokeBoolOutEncounter(t, director, "TryGetRandomEncounter", out encounter)) return true;
        if (TryInvokeBoolOutEncounter(t, director, "TryRollWorldEncounter", out encounter)) return true;
        if (TryInvokeBoolOutEncounter(t, director, "TryRoll", out encounter)) return true;

        // 2) EncounterDef GetRandomEncounter()
        if (TryInvokeReturnsEncounter(t, director, "GetRandomEncounter", out encounter)) return true;
        if (TryInvokeReturnsEncounter(t, director, "RollWorldEncounter", out encounter)) return true;

        return false;
    }

    private static bool TryInvokeBoolOutEncounter(Type t, object target, string methodName, out EncounterDef? encounter)
    {
        encounter = null;

        var mi = t.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
        if (mi == null) return false;

        var ps = mi.GetParameters();
        if (ps.Length == 1 && ps[0].ParameterType.IsByRef)
        {
            var args = new object?[] { null };
            var result = mi.Invoke(target, args);
            if (result is bool ok && ok && args[0] is EncounterDef enc)
            {
                encounter = enc;
                return true;
            }
        }

        return false;
    }

    private static bool TryInvokeReturnsEncounter(Type t, object target, string methodName, out EncounterDef? encounter)
    {
        encounter = null;

        var mi = t.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
        if (mi == null) return false;

        if (mi.GetParameters().Length != 0) return false;

        var result = mi.Invoke(target, null);
        if (result is EncounterDef enc)
        {
            encounter = enc;
            return true;
        }

        return false;
    }
}
