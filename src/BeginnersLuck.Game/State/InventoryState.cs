using System.Collections.Generic;
using BeginnersLuck.Game.Items;

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

    public bool AddItem(string itemId, int quantity, ItemDb db)
    {
        if (db == null || string.IsNullOrWhiteSpace(itemId) || quantity <= 0)
            return false;

        if (!db.IsKnown(itemId))
            return false;

        Add(itemId, quantity);
        return true;
    }

    public bool RemoveItem(string itemId, int quantity)
        => Remove(itemId, quantity);

    public int GetQuantity(string itemId)
        => CountOf(itemId);

    public IReadOnlyList<ItemStack> GetAllStacks(ItemDb db)
    {
        var list = new List<ItemStack>();

        foreach (var kv in Counts)
        {
            if (kv.Value <= 0)
                continue;

            int maxStack = db?.MaxStackOf(kv.Key) ?? 99;
            maxStack = maxStack <= 0 ? 99 : maxStack;

            int remaining = kv.Value;
            while (remaining > 0)
            {
                int chunk = remaining > maxStack ? maxStack : remaining;
                list.Add(new ItemStack(kv.Key, chunk));
                remaining -= chunk;
            }
        }

        return list;
    }

}
