using System;
using System.Collections.Generic;
using BeginnersLuck.Engine.Graphics;
using BeginnersLuck.Engine.Input;
using BeginnersLuck.Engine.Rendering;
using BeginnersLuck.Engine.UI;
using BeginnersLuck.Engine.Update;
using BeginnersLuck.Game.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BeginnersLuck.Game.Scenes;

public sealed class InventoryScene : PanelSceneBase
{
    // Sub-panels inside ContentRect
    private Rectangle _listRect;
    private Rectangle _detailsRect;

    private int _focusIndex;
    private int _scroll;

    // Cached view of inventory (rebuild when changes)
    private readonly List<(string ItemId, int Qty)> _items = new();

    public InventoryScene(GameServices s)
        : base(
            s,
            panelRect: new Rectangle(36, 26, 568, 308), // tuned for 640x360
            title: "INVENTORY")
    {
        FooterHint = "ENTER/A: USE   BACK/B: RETURN";
        TitleScale = 2;
        PanelPadding = 14;
    }

    protected override void OnLoad(Microsoft.Xna.Framework.Graphics.GraphicsDevice graphicsDevice, Microsoft.Xna.Framework.Content.ContentManager content)
    {
        RebuildList();
        _focusIndex = Math.Clamp(_focusIndex, 0, Math.Max(0, _items.Count - 1));
        _scroll = 0;
        ClampScroll();
    }

    protected override void OnUnload()
    {
        _items.Clear();
    }

    protected override void OnUpdate(UpdateContext uc)
    {
        if (_items.Count == 0)
            return;

        // Navigate (support repeat)
        var up = uc.Actions.Get(GameAction.MoveUp);
        if (up.Pressed || up.Repeated)
            MoveFocus(-1);

        var dn = uc.Actions.Get(GameAction.MoveDown);
        if (dn.Pressed || dn.Repeated)
            MoveFocus(+1);

        // Use item
        if (uc.Actions.Pressed(uc.Input, GameAction.Confirm))
        {
            var (id, qty) = _items[_focusIndex];
            if (qty > 0)
            {
                if (S.ItemUse.TryUse(id, out var msg))
                {
                    SetToast(msg.ToUpperInvariant(), 1.2f);

                    RebuildList();
                    _focusIndex = Math.Clamp(_focusIndex, 0, Math.Max(0, _items.Count - 1));
                    ClampScroll();
                }
                else
                {
                    SetToast(msg.ToUpperInvariant(), 1.1f);
                }
            }

            uc.Actions.ConsumeAll();
        }
    }

    protected override void DrawPanelContent(SpriteBatch sb, Rectangle content, RenderContext rc)
    {
        if (White == null) return;

        // Split content into left list + right details
        int gap = 10;
        int leftW = (int)(content.Width * 0.52f);
        _listRect = new Rectangle(content.X, content.Y, leftW, content.Height);
        _detailsRect = new Rectangle(content.X + leftW + gap, content.Y, content.Width - leftW - gap, content.Height);

        MenuRenderer.DrawPanel(sb, White, _listRect, new Color(12, 12, 20) * 0.98f);
        MenuRenderer.DrawPanel(sb, White, _detailsRect, new Color(12, 12, 20) * 0.98f);

        DrawList(sb);
        DrawDetails(sb);
    }

    private void MoveFocus(int delta)
    {
        if (_items.Count == 0) return;
        _focusIndex = Math.Clamp(_focusIndex + delta, 0, _items.Count - 1);
        ClampScroll();
    }

    private void ClampScroll()
    {
        int visible = VisibleRows();
        if (visible <= 0) visible = 1;

        if (_focusIndex < _scroll) _scroll = _focusIndex;
        if (_focusIndex >= _scroll + visible) _scroll = _focusIndex - visible + 1;

        int maxScroll = Math.Max(0, _items.Count - visible);
        _scroll = Math.Clamp(_scroll, 0, maxScroll);
    }

    private int VisibleRows()
    {
        int rowH = S.UiFont.LineHeight(1) + 8;
        return Math.Max(1, (_listRect.Height - 16) / rowH);
    }

    private void RebuildList()
    {
        _items.Clear();

        // Your current inventory type is Dictionary-backed (Counts).
        foreach (var kv in S.Player.Inventory.Counts)
        {
            if (kv.Value > 0)
                _items.Add((kv.Key, kv.Value));
        }

        // Stable order by name
        _items.Sort((a, b) =>
            string.Compare(S.Items.NameOf(a.ItemId), S.Items.NameOf(b.ItemId), StringComparison.OrdinalIgnoreCase));

        _focusIndex = Math.Clamp(_focusIndex, 0, Math.Max(0, _items.Count - 1));
        ClampScroll();
    }

    private void DrawList(SpriteBatch sb)
    {
        int rowH = S.UiFont.LineHeight(1) + 8;
        int visible = VisibleRows();

        int y = _listRect.Y + 10;

        if (_items.Count == 0)
        {
            S.UiFont.Draw(sb, "(EMPTY)", new Vector2(_listRect.X + 10, y), Color.White * 0.6f, 1);
            return;
        }

        for (int i = 0; i < visible; i++)
        {
            int idx = _scroll + i;
            if (idx >= _items.Count) break;

            var (id, qty) = _items[idx];
            bool focused = (idx == _focusIndex);

            bool canUse = S.ItemUse.CanUse(id, out var reason);

            var r = new Rectangle(_listRect.X + 8, y - 2, _listRect.Width - 16, rowH);

            var fill = focused ? new Color(70, 70, 120) : new Color(40, 40, 70);
            if (!canUse) fill = new Color(25, 25, 35);

            sb.Draw(White!, r, fill * 0.90f);

            var outline = focused ? Color.White * 0.55f : Color.White * 0.20f;
            if (!canUse) outline = Color.White * 0.12f;
            MenuRenderer.DrawOutline(sb, White!, r, 2, outline);

            string name = S.Items.NameOf(id).ToUpperInvariant();
            string right = $"x{qty}";

            var nameColor = canUse ? Color.White * 0.92f : Color.White * 0.45f;
            var qtyColor = canUse ? Color.White * 0.75f : Color.White * 0.35f;

            int rightW = S.UiFont.Measure(right, 1).X;
            int maxNameW = r.Width - 18 - rightW;

            name = S.UiFont.TrimToWidth(name, maxNameW, 1);

            S.UiFont.Draw(sb, name, new Vector2(r.X + 10, r.Y + 6), nameColor, 1);
            S.UiFont.Draw(sb, right, new Vector2(r.Right - 10 - rightW, r.Y + 6), qtyColor, 1);

            // Focused unusable reason tag
            if (focused && !canUse && !string.IsNullOrWhiteSpace(reason))
            {
                string tag = reason.ToUpperInvariant();
                int tagW = S.UiFont.Measure(tag, 1).X;
                S.UiFont.Draw(sb, tag, new Vector2(r.Right - 10 - tagW, r.Y + 6), Color.White * 0.25f, 1);
            }

            y += rowH;
        }
    }

    private void DrawDetails(SpriteBatch sb)
    {
        int x = _detailsRect.X + 12;
        int y = _detailsRect.Y + 12;

        if (_items.Count == 0)
        {
            S.UiFont.Draw(sb, "NO ITEMS.", new Vector2(x, y), Color.White * 0.7f, 1);
            return;
        }

        var (id, qty) = _items[_focusIndex];

        string name = S.Items.NameOf(id).ToUpperInvariant();
        S.UiFont.Draw(sb, name, new Vector2(x, y), Color.White * 0.95f, 1);
        y += S.UiFont.LineHeight(1) + 8;

        // Preview / effect
        string preview = S.Items.UsePreviewOf(id);
        S.UiFont.Draw(sb, preview, new Vector2(x, y), Color.White * 0.80f, 1);
        y += S.UiFont.LineHeight(1) + 10;

        // Description
        string desc = S.Items.DescOfOrFallback(id);
        if (string.IsNullOrWhiteSpace(desc)) desc = "NO DESCRIPTION.";

        int maxW = _detailsRect.Width - 24;
        foreach (var line in Wrap(desc.ToUpperInvariant(), maxW))
        {
            S.UiFont.Draw(sb, line, new Vector2(x, y), Color.White * 0.70f, 1);
            y += S.UiFont.LineHeight(1);
            if (y > _detailsRect.Bottom - 30) break;
        }

        // Qty bottom
        S.UiFont.Draw(sb, $"QTY: {qty}", new Vector2(x, _detailsRect.Bottom - 22), Color.White * 0.65f, 1);

        IEnumerable<string> Wrap(string text, int maxWidth)
        {
            text = text.Replace("\n", " ");
            while (text.Length > 0)
            {
                var candidate = S.UiFont.TrimToWidth(text, maxWidth, 1);
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
    }
}
