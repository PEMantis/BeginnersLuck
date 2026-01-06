using System.Collections.Generic;

namespace BeginnersLuck.Game.State;

public sealed class InventoryState
{
    public Dictionary<string, int> Counts { get; } = new();

    public bool TryGetCount(string id, out int qty) => Counts.TryGetValue(id, out qty);

    public int CountOf(string id)
        => (id != null && Counts.TryGetValue(id, out var qty)) ? qty : 0;

    public IEnumerable<(string Id, int Qty)> Enumerate()
    {
        foreach (var kv in Counts)
            yield return (kv.Key, kv.Value);
    }

    public void Add(string id, int delta)
    {
        if (string.IsNullOrWhiteSpace(id)) return;

        Counts.TryGetValue(id, out var cur);
        cur += delta;

        if (cur <= 0) Counts.Remove(id);
        else Counts[id] = cur;
    }

    public bool Remove(string id, int qty)
    {
        if (string.IsNullOrWhiteSpace(id) || qty <= 0) return false;
        if (!Counts.TryGetValue(id, out var cur) || cur < qty) return false;

        cur -= qty;
        if (cur <= 0) Counts.Remove(id);
        else Counts[id] = cur;

        return true;
    }
    public bool Has(string id, int qty = 1) => CountOf(id) >= qty;

}
