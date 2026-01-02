using System;
using System.Collections.Generic;
using BeginnersLuck.Engine.Update;
using BeginnersLuck.Game.Items;
using BeginnersLuck.Game.Services;

namespace BeginnersLuck.Game.Menu;

public sealed class InventoryPage : ListDetailsPageBase
{
    public override string Title => "INVENTORY";
    public override string FooterHint => "ENTER/A: USE   BACK/B: CLOSE";

    private readonly List<(string ItemId, int Qty)> _items = new();

    protected override void OnEnterPage(GameServices s)
    {
        RebuildList(s);
        FocusIndex = Math.Clamp(FocusIndex, 0, Math.Max(0, _items.Count - 1));
        Scroll = 0;
        ClampScroll(s);
    }

    protected override void OnUpdatePage(GameServices s, in UpdateContext uc)
    {
        // If inventory changes out of band later, you can detect and rebuild here.
        // For now, no-op.
    }

    protected override int ItemCount(GameServices s) => _items.Count;

    protected override void GetRow(GameServices s, int index, out string left, out string right)
    {
        var (id, qty) = _items[index];
        left = s.Items.NameOf(id);
        right = $"x{qty}";
    }

    protected override bool IsRowEnabled(GameServices s, int index, out string reason)
    {
        var (id, qty) = _items[index];

        reason = "";

        if (!s.Items.TryGet(id, out var def))
        {
            reason = "UNKNOWN";
            return false;
        }

        if (!def.Usable || def.Effect == UseEffect.None)
        {
            reason = "NOT USABLE";
            return false;
        }

        if (qty <= 0)
        {
            reason = "NONE";
            return false;
        }

        return true;
    }

    protected override void GetDetails(GameServices s, int index, out string title, out string preview, out string body, out string footerLine)
    {
        var (id, qty) = _items[index];

        title = s.Items.NameOf(id);

        preview = UsePreview(s, id);

        body = s.Items.DescOfOrFallback(id);

        footerLine = $"QTY: {qty}";
    }

    protected override void OnConfirm(GameServices s, in UpdateContext uc, int index)
    {
        if (_items.Count == 0) return;

        var (id, qty) = _items[index];
        if (qty <= 0) return;

        if (s.ItemUse.TryUse(id, out var msg))
        {
            SetToast(msg.ToUpperInvariant(), 1.2f);
            RebuildList(s);

            FocusIndex = Math.Clamp(FocusIndex, 0, Math.Max(0, _items.Count - 1));
            ClampScroll(s);
        }
        else
        {
            SetToast(msg.ToUpperInvariant(), 1.1f);
        }
    }

    private void RebuildList(GameServices s)
    {
        _items.Clear();

        foreach (var kv in s.Player.Inventory.Counts)
        {
            if (kv.Value > 0)
                _items.Add((kv.Key, kv.Value));
        }

        _items.Sort((a, b) =>
            string.Compare(s.Items.NameOf(a.ItemId), s.Items.NameOf(b.ItemId), StringComparison.OrdinalIgnoreCase));
    }

    private static string UsePreview(GameServices s, string itemId)
    {
        if (!s.Items.TryGet(itemId, out var def))
            return "UNKNOWN ITEM.";

        if (!def.Usable || def.Effect == UseEffect.None)
            return "NOT USABLE.";

        return def.Effect switch
        {
            UseEffect.HealHp => $"RESTORES {def.Amount} HP.",
            _ => "USABLE."
        };
    }
}
