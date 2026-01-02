using System;
using System.Collections.Generic;
using BeginnersLuck.Engine.Input;
using BeginnersLuck.Engine.UI;
using BeginnersLuck.Engine.Update;
using BeginnersLuck.Game.Items;
using BeginnersLuck.Game.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BeginnersLuck.Game.Menu;

public sealed class InventoryPage : IMenuPage
{
    public string Title => "INVENTORY";
    public string FooterHint => "ENTER/A: USE   BACK/B: CLOSE";

    // Layout inside hub content rect
    private Rectangle _listRect;
    private Rectangle _detailsRect;

    private int _focusIndex;
    private int _scroll;

    private readonly List<(string ItemId, int Qty)> _items = new();

    // Lightweight toast
    private string _toast = "";
    private float _toastT = 0f;

    // Press-through guard when hub switches focus into the page
    private bool _eatFirstUpdate = true;
    private int _cachedListHeight = 0;


    public void OnEnter(GameServices s)
    {
        RebuildList(s);

        _focusIndex = Math.Clamp(_focusIndex, 0, Math.Max(0, _items.Count - 1));
        _scroll = 0;

        // We don't know listRect yet until first Draw, but clamp anyway using fallback height
        ClampScroll(s);

        _toast = "";
        _toastT = 0f;

        // Important: page just became active (hub switched focus) → prevent confirm press-through
        _eatFirstUpdate = true;
    }


    public void OnExit(GameServices s)
    {
        _toast = "";
        _toastT = 0f;
    }

    public void Update(GameServices s, in UpdateContext uc)
    {
        if (_eatFirstUpdate)
        {
            _eatFirstUpdate = false;
            uc.Actions.ConsumeAll();
            return;
        }

        float dt = (float)uc.GameTime.ElapsedGameTime.TotalSeconds;
        if (_toastT > 0f) _toastT = MathF.Max(0f, _toastT - dt);

        if (_items.Count == 0)
            return;

        // Navigate with repeat
        var up = uc.Actions.Get(GameAction.MoveUp);
        if (up.Pressed || up.Repeated)
            MoveFocus(s, -1);

        var dn = uc.Actions.Get(GameAction.MoveDown);
        if (dn.Pressed || dn.Repeated)
            MoveFocus(s, +1);

        // Use item
        if (uc.Actions.Pressed(uc.Input, GameAction.Confirm))
        {
            var (id, qty) = _items[_focusIndex];
            if (qty > 0)
            {
                if (s.ItemUse.TryUse(id, out var msg))
                {
                    SetToast(msg.ToUpperInvariant(), 1.2f);

                    RebuildList(s);
                    _focusIndex = Math.Clamp(_focusIndex, 0, Math.Max(0, _items.Count - 1));
                    ClampScroll(s);
                }
                else
                {
                    SetToast(msg.ToUpperInvariant(), 1.1f);
                }
            }

            uc.Actions.ConsumeAll();
        }
    }

    public void Draw(GameServices s, SpriteBatch sb, Rectangle contentRect, float timeSeconds)
    {
        var white = s.PixelWhite;
        if (white == null) return;

        // Split content into list/details
        int pad = 10;
        int gap = 10;

        var inner = new Rectangle(
            contentRect.X + pad,
            contentRect.Y + pad,
            contentRect.Width - pad * 2,
            contentRect.Height - pad * 2
        );

        int leftW = (int)(inner.Width * 0.52f);
        _listRect = new Rectangle(inner.X, inner.Y, leftW, inner.Height);
        _detailsRect = new Rectangle(inner.X + leftW + gap, inner.Y, inner.Width - leftW - gap, inner.Height);

        // Cache height for scrolling math (used before first draw too)
        _cachedListHeight = _listRect.Height;

        MenuRenderer.DrawPanel(sb, white, _listRect, new Color(12, 12, 20) * 0.98f);
        MenuRenderer.DrawPanel(sb, white, _detailsRect, new Color(12, 12, 20) * 0.98f);

        DrawList(s, sb, white);
        DrawDetails(s, sb);

        if (_toastT > 0f)
            DrawToast(s, sb, white, contentRect);
    }

    private void SetToast(string text, float seconds)
    {
        _toast = text ?? "";
        _toastT = seconds;
    }

    private void MoveFocus(GameServices s, int delta)
    {
        if (_items.Count == 0) return;
        _focusIndex = Math.Clamp(_focusIndex + delta, 0, _items.Count - 1);
        ClampScroll(s);
    }

    private void ClampScroll(GameServices s)
    {
        int visible = VisibleRows(s);
        if (visible <= 0) visible = 1;

        if (_focusIndex < _scroll) _scroll = _focusIndex;
        if (_focusIndex >= _scroll + visible) _scroll = _focusIndex - visible + 1;

        int maxScroll = Math.Max(0, _items.Count - visible);
        _scroll = Math.Clamp(_scroll, 0, maxScroll);
    }

    private int VisibleRows(GameServices s)
    {
        int listH = _cachedListHeight > 0 ? _cachedListHeight : (_listRect.Height > 0 ? _listRect.Height : 160);
        int rowH = s.UiFont.LineHeight(1) + 8;
        return Math.Max(1, (listH - 16) / rowH);
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

    private bool CanUse(GameServices s, string itemId, out string reason)
    {
        reason = "";

        if (!s.Items.TryGet(itemId, out var def))
        {
            reason = "UNKNOWN";
            return false;
        }

        if (!def.Usable || def.Effect == UseEffect.None)
        {
            reason = "NOT USABLE";
            return false;
        }

        if (!s.Player.Inventory.TryGetCount(itemId, out var qty) || qty <= 0)
        {
            reason = "NONE";
            return false;
        }

        return true;
    }

    private string UsePreview(GameServices s, string itemId)
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

    private void DrawList(GameServices s, SpriteBatch sb, Texture2D white)
    {
        int rowH = s.UiFont.LineHeight(1) + 8;
        int visible = VisibleRows(s);

        int y = _listRect.Y + 10;

        if (_items.Count == 0)
        {
            s.UiFont.Draw(sb, "(EMPTY)", new Vector2(_listRect.X + 10, y), Color.White * 0.6f, 1);
            return;
        }

        for (int i = 0; i < visible; i++)
        {
            int idx = _scroll + i;
            if (idx >= _items.Count) break;

            var (id, qty) = _items[idx];
            bool focused = (idx == _focusIndex);

            bool canUse = CanUse(s, id, out var reason);

            var r = new Rectangle(_listRect.X + 8, y - 2, _listRect.Width - 16, rowH);

            var fill = focused ? new Color(70, 70, 120) : new Color(40, 40, 70);
            if (!canUse) fill = new Color(25, 25, 35);

            sb.Draw(white, r, fill * 0.90f);

            var outline = focused ? Color.White * 0.55f : Color.White * 0.20f;
            if (!canUse) outline = Color.White * 0.12f;
            MenuRenderer.DrawOutline(sb, white, r, 2, outline);

            string name = s.Items.NameOf(id).ToUpperInvariant();
            string right = $"x{qty}";

            var nameColor = canUse ? Color.White * 0.92f : Color.White * 0.45f;
            var qtyColor  = canUse ? Color.White * 0.75f : Color.White * 0.35f;

            int rightW = s.UiFont.Measure(right, 1).X;

            // If focused+unusable, show reason instead of qty (avoids overlap)
            if (focused && !canUse && !string.IsNullOrWhiteSpace(reason))
            {
                right = reason.ToUpperInvariant();
                rightW = s.UiFont.Measure(right, 1).X;
                qtyColor = Color.White * 0.25f;
            }

            int maxNameW = r.Width - 18 - rightW;
            name = s.UiFont.TrimToWidth(name, maxNameW, 1);

            s.UiFont.Draw(sb, name, new Vector2(r.X + 10, r.Y + 6), nameColor, 1);
            s.UiFont.Draw(sb, right, new Vector2(r.Right - 10 - rightW, r.Y + 6), qtyColor, 1);

            y += rowH;
        }
        var track = new Rectangle(_listRect.Right - 8, _listRect.Y + 10, 4, _listRect.Height - 20);
        MenuRenderer.DrawScrollBar(sb, white, track, _items.Count, _scroll, visible, alpha: 0.45f);

    }

    private void DrawDetails(GameServices s, SpriteBatch sb)
    {
        int x = _detailsRect.X + 12;
        int y = _detailsRect.Y + 12;

        if (_items.Count == 0)
        {
            s.UiFont.Draw(sb, "NO ITEMS.", new Vector2(x, y), Color.White * 0.7f, 1);
            return;
        }

        var (id, qty) = _items[_focusIndex];

        string name = s.Items.NameOf(id).ToUpperInvariant();
        s.UiFont.Draw(sb, name, new Vector2(x, y), Color.White * 0.95f, 1);
        y += s.UiFont.LineHeight(1) + 8;

        string preview = UsePreview(s, id);
        s.UiFont.Draw(sb, preview, new Vector2(x, y), Color.White * 0.80f, 1);
        y += s.UiFont.LineHeight(1) + 10;

        string desc = s.Items.DescOfOrFallback(id);
        if (string.IsNullOrWhiteSpace(desc)) desc = "NO DESCRIPTION.";

        int maxW = _detailsRect.Width - 24;
        foreach (var line in Wrap(s, desc.ToUpperInvariant(), maxW))
        {
            s.UiFont.Draw(sb, line, new Vector2(x, y), Color.White * 0.70f, 1);
            y += s.UiFont.LineHeight(1);
            if (y > _detailsRect.Bottom - 30) break;
        }

        s.UiFont.Draw(sb, $"QTY: {qty}", new Vector2(x, _detailsRect.Bottom - 22), Color.White * 0.65f, 1);
    }

    private static IEnumerable<string> Wrap(GameServices s, string text, int maxWidth)
    {
        text = text.Replace("\n", " ");
        while (text.Length > 0)
        {
            var candidate = s.UiFont.TrimToWidth(text, maxWidth, 1);
            if (candidate.Length == text.Length)
            {
                yield return candidate;
                yield break;
            }

            int cut = candidate.LastIndexOf(' ');
            if (cut > 10)
                candidate = candidate[..cut];

            yield return candidate.TrimEnd();
            text = text[candidate.Length..].TrimStart();
        }
    }

    private void DrawToast(GameServices s, SpriteBatch sb, Texture2D white, Rectangle contentRect)
    {
        // centered inside page area
        var r = new Rectangle(contentRect.X + 30, contentRect.Y + 10, contentRect.Width - 60, 20);
        sb.Draw(white, r, new Color(10, 10, 18) * 0.88f);
        MenuRenderer.DrawOutline(sb, white, r, 1, Color.White * 0.25f);

        var size = s.UiFont.Measure(_toast, 1);
        var pos = new Vector2(r.X + (r.Width - size.X) / 2, r.Y + 3);
        s.UiFont.Draw(sb, _toast, pos, Color.White * 0.92f, 1);
    }
}
