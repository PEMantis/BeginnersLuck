using System;
using System.Collections.Generic;

namespace BeginnersLuck.Game.Encounters;

public readonly record struct WeightedEncounter(string EncounterId, int Weight);

public sealed class EncounterTables
{
    private readonly Dictionary<string, WeightedEncounter[]> _tables;

    public EncounterTables(Dictionary<string, WeightedEncounter[]> tables)
        => _tables = tables;

    public WeightedEncounter[] Get(string id)
        => _tables.TryGetValue(id, out var t) ? t : Array.Empty<WeightedEncounter>();

    public static EncounterTables CreateDefault()
    {
        return new EncounterTables(new Dictionary<string, WeightedEncounter[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["none"] = Array.Empty<WeightedEncounter>(),

            ["plains_low"]  = new[] { new WeightedEncounter("slimes_easy", 100) },
            ["plains_high"] = new[] { new WeightedEncounter("slimes_easy", 100) },
            ["road"]        = new[] { new WeightedEncounter("slimes_easy", 100) },
            ["mountain"]    = new[] { new WeightedEncounter("slimes_easy", 100) },
        });
    }
}
