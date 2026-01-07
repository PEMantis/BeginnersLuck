using System;
using System.Collections.Generic;
using BeginnersLuck.Game.Stats;

namespace BeginnersLuck.Game.Status;

/// <summary>
/// Flat stat modifiers: +3 ATK, -2 DEF, etc.
/// (We keep it simple now; add percent mods later if needed.)
/// </summary>
public sealed class StatModifier
{
    private readonly Dictionary<StatType, int> _flat = new();

    public IReadOnlyDictionary<StatType, int> Flat => _flat;

    public void AddFlat(StatType stat, int delta)
    {
        if (delta == 0) return;
        _flat[stat] = _flat.TryGetValue(stat, out var cur) ? (cur + delta) : delta;

        if (_flat[stat] == 0)
            _flat.Remove(stat);
    }

    public int GetFlat(StatType stat) => _flat.TryGetValue(stat, out var v) ? v : 0;

    public static StatModifier FromSingle(StatType stat, int delta)
    {
        var m = new StatModifier();
        m.AddFlat(stat, delta);
        return m;
    }
}
