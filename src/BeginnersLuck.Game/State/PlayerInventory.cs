using System.Collections.Generic;

namespace BeginnersLuck.Game.State;

public sealed class PlayerInventory
{
    public Dictionary<string, int> Counts { get; } = new();

    public bool TryGetCount(string id, out int qty) => Counts.TryGetValue(id, out qty);

    public void Add(string id, int delta)
    {
        if (string.IsNullOrWhiteSpace(id)) return;

        Counts.TryGetValue(id, out var cur);
        cur += delta;

        if (cur <= 0) Counts.Remove(id);
        else Counts[id] = cur;
    }
}
