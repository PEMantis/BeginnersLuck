using System.Collections.Generic;

namespace BeginnersLuck.Game.Items;

public sealed class ItemDb
{
    private readonly Dictionary<string, string> _names = new();

    public void Add(string itemId, string displayName)
    {
        if (string.IsNullOrWhiteSpace(itemId)) return;
        _names[itemId] = string.IsNullOrWhiteSpace(displayName) ? itemId : displayName;
    }

    public string NameOf(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId)) return "(null)";
        return _names.TryGetValue(itemId, out var n) ? n : itemId;
    }
}
