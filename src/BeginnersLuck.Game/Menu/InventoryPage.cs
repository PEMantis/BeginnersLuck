using System;
using System.Collections.Generic;
using System.Linq;
using BeginnersLuck.Engine.UI;
using BeginnersLuck.Game.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BeginnersLuck.Game.Menu;

public sealed class InventoryPage : IMenuPage
{
    public string Title => "INVENTORY";

    private int _selected;
    private int _scroll;

    private KeyboardState _prevKs;
    private GamePadState _prevPad;
    private bool _eatFirstUpdate = true;

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
        var ks = Keyboard.GetState();
        var pad = GamePad.GetState(PlayerIndex.One);

        if (_eatFirstUpdate)
        {
            _eatFirstUpdate = false;
            _prevKs = ks;
            _prevPad = pad;
            return;
        }

        if (_items.Count == 0)
        {
            _prevKs = ks;
            _prevPad = pad;
            return;
        }

        int visibleRows = 8;
        int maxSel = _items.Count - 1;

        if (Pressed(ks, Keys.Up) || Pressed(ks, Keys.W) || Pressed(pad, Buttons.DPadUp))
            _selected = Math.Max(0, _selected - 1);

        if (Pressed(ks, Keys.Down) || Pressed(ks, Keys.S) || Pressed(pad, Buttons.DPadDown))
            _selected = Math.Min(maxSel, _selected + 1);

        if (Pressed(ks, Keys.PageUp))
            _selected = Math.Max(0, _selected - visibleRows);

        if (Pressed(ks, Keys.PageDown))
            _selected = Math.Min(maxSel, _selected + visibleRows);

        _scroll = ClampScroll(_scroll, _selected, visibleRows, _items.Count);

        // ✅ USE ITEM + TOAST
        if (Pressed(ks, Keys.Enter) || Pressed(pad, Buttons.A))
        {
            var id = _items[_selected].Id;

            if (s.ItemUse.TryUse(id, out var msg))
            {
                s.Toasts.Push(msg);
                Rebuild(s); // qty changed
                _selected = Math.Clamp(_selected, 0, Math.Max(0, _items.Count - 1));
                _scroll = ClampScroll(_scroll, _selected, visibleRows, _items.Count);
            }
            else
            {
                s.Toasts.Push(msg);
            }
        }

        _prevKs = ks;
        _prevPad = pad;
    }

    public void Draw(GameServices s, SpriteBatch sb, Rectangle contentRect, float timeSeconds)
    {
        var listRect = new Rectangle(contentRect.X + 8, contentRect.Y + 6, 150, contentRect.Height - 36);
        var detailsRect = new Rectangle(listRect.Right + 8, contentRect.Y + 6, contentRect.Width - (listRect.Width + 24), contentRect.Height - 36);
        var actionsRect = new Rectangle(contentRect.X + 8, contentRect.Bottom - 26, contentRect.Width - 16, 20);

        DrawSoftPanel(s, sb, listRect);
        DrawSoftPanel(s, sb, detailsRect);
        DrawSoftPanel(s, sb, actionsRect);

        if (_items.Count == 0)
        {
            s.UiFont.Draw(sb, "(EMPTY)", new Vector2(listRect.X + 10, listRect.Y + 10), Color.White * 0.65f, 1);
            s.UiFont.Draw(sb, "Go win some loot.", new Vector2(detailsRect.X + 10, detailsRect.Y + 10), Color.White * 0.55f, 1);
            return;
        }

        _selected = Math.Clamp(_selected, 0, _items.Count - 1);

        int rowH = 14;
        int visibleRows = Math.Max(1, (listRect.Height - 12) / rowH);
        _scroll = ClampScroll(_scroll, _selected, visibleRows, _items.Count);

        int start = _scroll;
        int end = Math.Min(_items.Count, start + visibleRows);

        int y = listRect.Y + 8;
        for (int i = start; i < end; i++)
        {
            bool sel = (i == _selected);

            if (sel)
            {
                var hl = new Rectangle(listRect.X + 4, y - 2, listRect.Width - 8, rowH);
                sb.Draw(s.PixelWhite, hl, new Color(120, 160, 255) * 0.18f);
                sb.Draw(s.PixelWhite, new Rectangle(hl.X, hl.Y, 2, hl.Height), new Color(170, 210, 255) * 0.60f);
            }

            var it = _items[i];

            s.UiFont.Draw(sb, it.Name.ToUpperInvariant(), new Vector2(listRect.X + 10, y), Color.White * (sel ? 0.95f : 0.75f), 1);

            string qty = $"x{it.Qty}";
            int qtyW = qty.Length * 8;
            s.UiFont.Draw(sb, qty, new Vector2(listRect.Right - 10 - qtyW, y), Color.White * (sel ? 0.85f : 0.55f), 1);

            y += rowH;
        }

        if (_items.Count > visibleRows)
        {
            string hint = $"{_selected + 1}/{_items.Count}";
            s.UiFont.Draw(sb, hint, new Vector2(listRect.Right - 10 - hint.Length * 8, listRect.Bottom - 12), Color.White * 0.45f, 1);
        }

        var selItem = _items[_selected];

        s.UiFont.Draw(sb, selItem.Name.ToUpperInvariant(), new Vector2(detailsRect.X + 10, detailsRect.Y + 8), Color.White * 0.92f, 2);
        s.UiFont.Draw(sb, $"QTY: {selItem.Qty}", new Vector2(detailsRect.X + 10, detailsRect.Y + 28), Color.White * 0.65f, 1);

        sb.Draw(s.PixelWhite, new Rectangle(detailsRect.X + 10, detailsRect.Y + 40, detailsRect.Width - 20, 1), Color.White * 0.12f);

        DrawWrapped(s, sb, selItem.Desc, new Rectangle(detailsRect.X + 10, detailsRect.Y + 46, detailsRect.Width - 20, detailsRect.Height - 56), Color.White * 0.60f, 1);

        string actions = "ENTER/A: USE";
        s.UiFont.Draw(sb, actions, new Vector2(actionsRect.X + 10, actionsRect.Y + 6), Color.White * 0.60f, 1);
    }

    // ---------- helpers ----------

    private void Rebuild(GameServices s)
    {
        _items.Clear();

        foreach (var kv in s.Player.Inventory.Counts)
        {
            string id = kv.Key;
            int qty = kv.Value;
            if (qty <= 0) continue;

            string name = SafeName(s, id);
            string desc = s.Items.DescOfOrFallback(id);

            _items.Add((id, name, desc, qty));
        }

        _items = _items.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToList();

        _selected = Math.Clamp(_selected, 0, Math.Max(0, _items.Count - 1));
        _scroll = 0;
    }

    private static string SafeName(GameServices s, string id)
    {
        try { return s.Items.NameOf(id); }
        catch { return id; }
    }

    private static int ClampScroll(int scroll, int selected, int visibleRows, int total)
    {
        int maxScroll = Math.Max(0, total - visibleRows);
        if (selected < scroll) scroll = selected;
        if (selected >= scroll + visibleRows) scroll = selected - visibleRows + 1;
        return Math.Clamp(scroll, 0, maxScroll);
    }

    private bool Pressed(KeyboardState ks, Keys k) => ks.IsKeyDown(k) && !_prevKs.IsKeyDown(k);
    private bool Pressed(GamePadState pad, Buttons b) => pad.IsButtonDown(b) && !_prevPad.IsButtonDown(b);

    private static void DrawSoftPanel(GameServices s, SpriteBatch sb, Rectangle r)
    {
        sb.Draw(s.PixelWhite, r, new Color(10, 10, 18) * 0.25f);
        sb.Draw(s.PixelWhite, new Rectangle(r.X, r.Y, r.Width, 1), Color.White * 0.08f);
        sb.Draw(s.PixelWhite, new Rectangle(r.X, r.Bottom - 1, r.Width, 1), Color.Black * 0.35f);
        sb.Draw(s.PixelWhite, new Rectangle(r.X, r.Y, 1, r.Height), Color.White * 0.06f);
        sb.Draw(s.PixelWhite, new Rectangle(r.Right - 1, r.Y, 1, r.Height), Color.Black * 0.35f);
    }

    private static void DrawWrapped(GameServices s, SpriteBatch sb, string text, Rectangle r, Color color, int scale)
    {
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
                s.UiFont.Draw(sb, line.ToUpperInvariant(), new Vector2(x, y), color, scale);
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
            s.UiFont.Draw(sb, line.ToUpperInvariant(), new Vector2(x, y), color, scale);
    }
}
