using System;
using System.Collections.Generic;
using System.Linq;
using BeginnersLuck.Engine.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using BeginnersLuck.Game.Services;

namespace BeginnersLuck.Game.Menu;

public sealed class InventoryPage : IMenuPage
{
    public string Title => "INVENTORY";

    private int _selected;
    private int _scroll;

    private KeyboardState _prevKs;
    private GamePadState _prevPad;
    private bool _eatFirstUpdate = true;

    // cached view for stable ordering
    private List<(string Id, string Name, string Desc, int Qty)> _items = new();

    public void OnEnter(GameServices s)
    {
        _selected = 0;
        _scroll = 0;
        _eatFirstUpdate = true;
        _prevKs = Keyboard.GetState();
        _prevPad = GamePad.GetState(PlayerIndex.One);
        Rebuild(s);
    }

    public void OnExit(GameServices s) { }

    public void Update(GameServices s, float dt)
    {
        // InventoryPage handles list selection input itself (v2)
        var ks = Keyboard.GetState();
        var pad = GamePad.GetState(PlayerIndex.One);

        if (_eatFirstUpdate)
        {
            _eatFirstUpdate = false;
            _prevKs = ks;
            _prevPad = pad;
            return;
        }

        // If your inventory can change elsewhere, you can rebuild occasionally.
        // For now, rebuild only if empty but player has items, etc.
        // (No-op unless you want it.)
        // Rebuild(s);

        if (_items.Count == 0)
        {
            _prevKs = ks;
            _prevPad = pad;
            return;
        }

        int visibleRows = GetVisibleRows();
        int maxSel = _items.Count - 1;

        // Move selection
        if (Pressed(ks, Keys.Up) || Pressed(ks, Keys.W) || Pressed(pad, Buttons.DPadUp))
            _selected = Math.Max(0, _selected - 1);

        if (Pressed(ks, Keys.Down) || Pressed(ks, Keys.S) || Pressed(pad, Buttons.DPadDown))
            _selected = Math.Min(maxSel, _selected + 1);

        // Page jump
        if (Pressed(ks, Keys.PageUp))
            _selected = Math.Max(0, _selected - visibleRows);

        if (Pressed(ks, Keys.PageDown))
            _selected = Math.Min(maxSel, _selected + visibleRows);

        // Keep selection visible
        _scroll = ClampScroll(_scroll, _selected, visibleRows, _items.Count);

        // "Use" (placeholder)
        if (Pressed(ks, Keys.Enter) || Pressed(pad, Buttons.A))
        {
            // Later: use the item and update inventory.
            // For now: just a safe no-op.
        }

        _prevKs = ks;
        _prevPad = pad;
    }

    public void Draw(GameServices s, SpriteBatch sb, Rectangle contentRect, float timeSeconds)
    {
        // Layout inside contentRect:
        // Left list area + right details area + bottom action strip

        var listRect = new Rectangle(contentRect.X + 8, contentRect.Y + 6, 150, contentRect.Height - 36);
        var detailsRect = new Rectangle(listRect.Right + 8, contentRect.Y + 6, contentRect.Width - (listRect.Width + 24), contentRect.Height - 36);
        var actionsRect = new Rectangle(contentRect.X + 8, contentRect.Bottom - 26, contentRect.Width - 16, 20);

        // Panels
        // (Assumes MenuHub already drew a backing rect; we add inner panels for structure.)
        DrawSoftPanel(s, sb, listRect);
        DrawSoftPanel(s, sb, detailsRect);
        DrawSoftPanel(s, sb, actionsRect);

        RebuildIfNeeded(s);

        if (_items.Count == 0)
        {
            s.Font.Draw(sb, "(EMPTY)", new Vector2(listRect.X + 10, listRect.Y + 10), Color.White * 0.65f, 1);
            s.Font.Draw(sb, "Go win some loot.", new Vector2(detailsRect.X + 10, detailsRect.Y + 10), Color.White * 0.55f, 1);
            return;
        }

        _selected = Math.Clamp(_selected, 0, _items.Count - 1);

        int rowH = 14;               // 8px font with spacing
        int visibleRows = Math.Max(1, (listRect.Height - 12) / rowH);
        _scroll = ClampScroll(_scroll, _selected, visibleRows, _items.Count);

        // Draw list rows
        int start = _scroll;
        int end = Math.Min(_items.Count, start + visibleRows);

        int y = listRect.Y + 8;
        for (int i = start; i < end; i++)
        {
            bool sel = (i == _selected);

            if (sel)
            {
                // highlight strip
                var hl = new Rectangle(listRect.X + 4, y - 2, listRect.Width - 8, rowH);
                sb.Draw(s.PixelWhite, hl, new Color(120, 160, 255) * 0.18f);
                sb.Draw(s.PixelWhite, new Rectangle(hl.X, hl.Y, 2, hl.Height), new Color(170, 210, 255) * 0.60f);
            }

            var it = _items[i];

            // Name
            s.Font.Draw(sb, it.Name.ToUpperInvariant(), new Vector2(listRect.X + 10, y), Color.White * (sel ? 0.95f : 0.75f), 1);

            // Qty right-aligned
            string qty = $"x{it.Qty}";
            int qtyW = qty.Length * 8;
            s.Font.Draw(sb, qty, new Vector2(listRect.Right - 10 - qtyW, y), Color.White * (sel ? 0.85f : 0.55f), 1);

            y += rowH;
        }

        // Scroll hint
        if (_items.Count > visibleRows)
        {
            string hint = $"{_selected + 1}/{_items.Count}";
            s.Font.Draw(sb, hint, new Vector2(listRect.Right - 10 - hint.Length * 8, listRect.Bottom - 12), Color.White * 0.45f, 1);
        }

        // Details panel for selected item
        var selItem = _items[_selected];

        s.Font.Draw(sb, selItem.Name.ToUpperInvariant(), new Vector2(detailsRect.X + 10, detailsRect.Y + 8), Color.White * 0.92f, 2);

        // Subline
        s.Font.Draw(sb, $"QTY: {selItem.Qty}", new Vector2(detailsRect.X + 10, detailsRect.Y + 28), Color.White * 0.65f, 1);

        // Divider
        sb.Draw(s.PixelWhite, new Rectangle(detailsRect.X + 10, detailsRect.Y + 40, detailsRect.Width - 20, 1), Color.White * 0.12f);

        // Wrapped description
        DrawWrapped(s, sb, selItem.Desc, new Rectangle(detailsRect.X + 10, detailsRect.Y + 46, detailsRect.Width - 20, detailsRect.Height - 56), Color.White * 0.60f, 1);

        // Action strip
        // (Use/Drop are placeholders for now)
        string actions = "ENTER/A: USE    (LATER) X/Y: DROP";
        s.Font.Draw(sb, actions, new Vector2(actionsRect.X + 10, actionsRect.Y + 6), Color.White * 0.60f, 1);
    }

    // -------- helpers --------

    private void Rebuild(GameServices s)
    {
        _items.Clear();

        foreach (var kv in s.Player.Inventory.Counts)
        {
            string id = kv.Key;
            int qty = kv.Value;
            if (qty <= 0) continue;

            string name;
            try { name = s.Items.NameOf(id); }
            catch { name = id; }

            string desc = $"A {name.ToLowerInvariant()}.";

            _items.Add((id, name, desc, qty));
        }

        _items = _items
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        _selected = Math.Clamp(_selected, 0, Math.Max(0, _items.Count - 1));
        _scroll = 0;
    }


    private void RebuildIfNeeded(GameServices s)
    {
        // Simple cheap check: count mismatch or selected out of range
        // If you want always consistent: just call Rebuild(s) every draw (fine for tiny inventories).
        // Here we keep it stable unless it looks invalid.
        if (_items.Count == 0 && s.Player.Inventory.Counts.Count > 0)
        {
            Rebuild(s);
            return;
        }

        if (_selected >= _items.Count && _items.Count > 0)
            _selected = _items.Count - 1;
    }

    private static int ClampScroll(int scroll, int selected, int visibleRows, int total)
    {
        int maxScroll = Math.Max(0, total - visibleRows);
        if (selected < scroll) scroll = selected;
        if (selected >= scroll + visibleRows) scroll = selected - visibleRows + 1;
        return Math.Clamp(scroll, 0, maxScroll);
    }

    private int GetVisibleRows()
    {
        // matches listRect math roughly; used for page up/down
        return 8;
    }

    private bool Pressed(KeyboardState ks, Keys k) => ks.IsKeyDown(k) && !_prevKs.IsKeyDown(k);
    private bool Pressed(GamePadState pad, Buttons b) => pad.IsButtonDown(b) && !_prevPad.IsButtonDown(b);

    private static void DrawSoftPanel(GameServices s, SpriteBatch sb, Rectangle r)
    {
        // requires a 1x1 white pixel. If you don't have s.PixelWhite, see note below.
        sb.Draw(s.PixelWhite, r, new Color(10, 10, 18) * 0.25f);
        sb.Draw(s.PixelWhite, new Rectangle(r.X, r.Y, r.Width, 1), Color.White * 0.08f);
        sb.Draw(s.PixelWhite, new Rectangle(r.X, r.Bottom - 1, r.Width, 1), Color.Black * 0.35f);
        sb.Draw(s.PixelWhite, new Rectangle(r.X, r.Y, 1, r.Height), Color.White * 0.06f);
        sb.Draw(s.PixelWhite, new Rectangle(r.Right - 1, r.Y, 1, r.Height), Color.Black * 0.35f);
    }

    private static void DrawWrapped(GameServices s, SpriteBatch sb, string text, Rectangle r, Color color, int scale)
    {
        // Super simple word wrap for 8x8 font:
        // width in chars = pixels / (8*scale)
        int charsPerLine = Math.Max(1, r.Width / (8 * scale));
        int lineH = 10 * scale;

        var words = (text ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string line = "";
        int x = r.X;
        int y = r.Y;

        foreach (var w in words)
        {
            string test = line.Length == 0 ? w : (line + " " + w);
            if (test.Length > charsPerLine)
            {
                s.Font.Draw(sb, line.ToUpperInvariant(), new Vector2(x, y), color, scale);
                y += lineH;
                if (y > r.Bottom - lineH) return;
                line = w;
            }
            else
            {
                line = test;
            }
        }

        if (line.Length > 0 && y <= r.Bottom - lineH)
            s.Font.Draw(sb, line.ToUpperInvariant(), new Vector2(x, y), color, scale);
    }

    private static string SafeName(GameServices s, string id)
    {
        try
        {
            return s.Items.NameOf(id);
        }
        catch
        {
            return $"ITEM {id}";
        }
    }

    private static string SafeDesc(GameServices s, string id, string name)
    {
        return $"A {name.ToLowerInvariant()}.";
    }

}
