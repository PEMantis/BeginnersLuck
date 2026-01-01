using System;
using BeginnersLuck.Game.World;
using Microsoft.Xna.Framework;

namespace BeginnersLuck.Game.Encounters;

public sealed class EncounterDirector
{
    private readonly IEncounterSource _source;

    private int _cooldownSteps;
    private int _stepsSinceLast;

    public int CooldownStepsAfterEncounter { get; set; } = 4;

    // Base chance per step at Danger=0. Danger adds on top.
    public float BaseChancePerStep { get; set; } = 0.03f; // 3%
    public float DangerBonusPerStep { get; set; } = 0.02f; // +2% per danger

    public int CooldownRemainingSteps => _cooldownSteps;

    public EncounterDirector(IEncounterSource source)
    {
        _source = source;
    }

    public EncounterIntent? OnPlayerMoved(Point newCell, ZoneInfo zone, Random rng)
    {
        _stepsSinceLast++;

        if (_cooldownSteps > 0)
        {
            _cooldownSteps--;
            return null;
        }

        if (zone.EncounterTableId == "none" || zone.Id == ZoneId.None)
            return null;

        // Compute chance
        float chance = ComputeChancePerStep(zone);
        chance = MathHelper.Clamp(chance, 0f, 0.60f);

        if (rng.NextDouble() > chance)
            return null;

        // Trigger encounter
        var enc = _source.PickEncounter(zone, rng);

        // If source returns "nothing", treat as no encounter
        if (enc.Enemies.Length == 0)
            return null;

        _cooldownSteps = CooldownStepsAfterEncounter;
        _stepsSinceLast = 0;

        return new EncounterIntent(enc, zone, newCell);
    }

    public float ComputeChancePerStep(ZoneInfo zone)
    {
        if (zone.EncounterTableId == "none" || zone.Id == ZoneId.None)
            return 0f;

        float chance = BaseChancePerStep + zone.Danger * DangerBonusPerStep;
        return MathHelper.Clamp(chance, 0f, 0.60f);
    }
}
