using System.Collections.Generic;

namespace BeginnersLuck.Game.State;

public sealed class Inventory
{
    private readonly Dictionary<string, int> _counts = new();

    public IReadOnlyDictionary<string, int> Counts => _counts;

    public int Get(string itemId)
        => _counts.TryGetValue(itemId, out var n) ? n : 0;

    public void Add(string itemId, int qty)
    {
        if (string.IsNullOrWhiteSpace(itemId) || qty <= 0) return;
        _counts[itemId] = Get(itemId) + qty;
    }
}
