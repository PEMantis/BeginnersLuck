using System;
using System.Collections.Generic;
using System.Linq;
using BeginnersLuck.Game.Stats;

namespace BeginnersLuck.Game.Status;

/// <summary>
/// Owns active timed effects and provides aggregated stat modifiers.
/// </summary>
public sealed class StatusController
{
    private readonly List<ActiveStatus> _active = new();

    public IReadOnlyList<ActiveStatus> Active => _active;

    public void AddOrRefresh(string id, StatModifier mods, int durationTurns, string sourceTag = "")
    {
        // Simple rule for V1:
        // - If same id exists, refresh duration to max(existing, new)
        // - Mods are replaced (so reapplying "Haste" won't double-stack unless you want it later)
        var existing = _active.FirstOrDefault(x => x.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            _active.Remove(existing);
        }

        _active.Add(new ActiveStatus(id, mods, durationTurns, sourceTag));
    }

    public void Remove(string id)
    {
        _active.RemoveAll(x => x.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    }

    public void TickEndOfTurn()
    {
        for (int i = 0; i < _active.Count; i++)
            _active[i].TickDown();

        _active.RemoveAll(x => x.Expired);
    }

    public int GetFlatMod(StatType stat)
    {
        int sum = 0;
        for (int i = 0; i < _active.Count; i++)
            sum += _active[i].Mods.GetFlat(stat);
        return sum;
    }
}
