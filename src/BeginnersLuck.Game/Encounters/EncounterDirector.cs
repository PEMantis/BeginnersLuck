using System;
using BeginnersLuck.Game.World;
using Microsoft.Xna.Framework;

namespace BeginnersLuck.Game.Encounters;

public sealed class EncounterDirector
{
    private readonly IEncounterSource _source;

    private int _cooldownSteps;
    private int _stepsSinceLast; // counts moves since last encounter

    public int CooldownStepsAfterEncounter { get; set; } = 4;

    // Base chance per step at Danger=0. Danger adds on top.
    public float BaseChancePerStep { get; set; } = 0.03f;  // 3%
    public float DangerBonusPerStep { get; set; } = 0.02f; // +2% per danger

    // ✅ New: ramp chance the longer you go without an encounter (anti-dry-streak).
    // Example: 0.01 means +1% per step since last encounter.
    public float PityRampPerStep { get; set; } = 0.01f;

    // Hard clamp so it never becomes silly.
    public float MaxChancePerStep { get; set; } = 0.60f;

    public int CooldownRemainingSteps => _cooldownSteps;
    public int StepsSinceLastEncounter => _stepsSinceLast;

    public EncounterDirector(IEncounterSource source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
    }

    public EncounterIntent? OnPlayerMoved(Point newCell, ZoneInfo zone, Random rng)
    {
        if (rng == null) throw new ArgumentNullException(nameof(rng));

        _stepsSinceLast++;

        if (_cooldownSteps > 0)
        {
            _cooldownSteps--;
            return null;
        }

        if (zone.EncounterTableId == "none" || zone.Id == ZoneId.None)
            return null;

        float chance = ComputeChancePerStep(zone);

        if (rng.NextDouble() > chance)
            return null;

        var enc = _source.PickEncounter(zone, rng);

        // If source returns "nothing", treat as no encounter.
        if (enc == null || enc.Enemies == null || enc.Enemies.Length == 0)
            return null;

        _cooldownSteps = CooldownStepsAfterEncounter;
        _stepsSinceLast = 0;

        return new EncounterIntent(enc, zone, newCell);
    }

    public float ComputeChancePerStep(ZoneInfo zone)
    {
        if (zone.EncounterTableId == "none" || zone.Id == ZoneId.None)
            return 0f;

        // Base + danger
        float chance = BaseChancePerStep + (zone.Danger * DangerBonusPerStep);

        // ✅ Pity ramp
        // After 10 steps: +10% if PityRampPerStep=0.01
        chance += _stepsSinceLast * PityRampPerStep;

        return MathHelper.Clamp(chance, 0f, MaxChancePerStep);
    }
}
