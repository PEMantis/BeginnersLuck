using System;
using System.Collections.Generic;
using BeginnersLuck.Engine.Graphics;
using BeginnersLuck.Engine.Input;
using BeginnersLuck.Engine.Rendering;
using BeginnersLuck.Engine.Scenes;
using BeginnersLuck.Engine.UI;
using BeginnersLuck.Engine.Update;
using BeginnersLuck.Game.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace BeginnersLuck.Game.Scenes;

public sealed class InventoryScene : SceneBase
{
    private readonly GameServices _s;
    private Texture2D? _white;

    // Layout (virtual 480x270)
    private readonly Rectangle _panel = new(28, 22, 424, 226);
    private readonly Rectangle _panelList = new(40, 56, 200, 150);
    private readonly Rectangle _panelDetails = new(250, 56, 190, 150);
    private readonly Rectangle _panelHint = new(40, 212, 400, 28);

    private int _focusIndex;
    private int _scroll;
    private bool _eatFirstUpdate = true;

    private string _toast = "";
    private float _toastT = 0f;

    // Cached view of inventory (rebuild when changes)
    private readonly List<(string ItemId, int Qty)> _items = new();

    public InventoryScene(GameServices s)
    {
        _s = s ?? throw new ArgumentNullException(nameof(s));
    }

    public override void Load(GraphicsDevice graphicsDevice, ContentManager content)
    {
        _white = new Texture2D(graphicsDevice, 1, 1);
        _white.SetData(new[] { Color.White });

        RebuildList();

        _focusIndex = Math.Clamp(_focusIndex, 0, Math.Max(0, _items.Count - 1));
        _scroll = 0;
        _eatFirstUpdate = true;
    }

    public override void Unload()
    {
        _white?.Dispose();
        _white = null;
        _items.Clear();
    }

    public override void Update(UpdateContext uc)
    {
        if (_eatFirstUpdate)
        {
            // Prevent “press-through” from Pause confirm
            _eatFirstUpdate = false;
            uc.Actions.ConsumeAll();
            return;
        }

        float dt = (float)uc.GameTime.ElapsedGameTime.TotalSeconds;
        if (_toastT > 0f) _toastT = MathF.Max(0f, _toastT - dt);

        // Cancel closes inventory
        if (uc.Actions.Pressed(uc.Input, GameAction.Cancel))
        {
            uc.Actions.ConsumeAll();
            _s.Scenes.Pop();
            return;
        }

        // Nothing to select
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
                // Try use (consumes 1 if successful)
                if (_s.ItemUse.TryUseOne(id))
                {
                    _toast = $"{_s.Items.NameOf(id).ToUpperInvariant()} USED!";
                    _toastT = 1.2f;

                    // inventory changed -> rebuild list & keep focus sane
                    RebuildList();
                    _focusIndex = Math.Clamp(_focusIndex, 0, Math.Max(0, _items.Count - 1));
                    ClampScroll();
                }
                else
                {
                    _toast = "CAN'T USE THAT.";
                    _toastT = 1.0f;
                }
            }

            uc.Actions.ConsumeAll();
        }
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
        // Row height is based on UI font line height, plus padding
        int rowH = _s.UiFont.LineHeight(1) + 6;
        return Math.Max(1, _panelList.Height / rowH);
    }

    private void RebuildList()
    {
        _items.Clear();

        // EXPECTATION: your inventory can be enumerated as (id, qty).
        // If your Inventory class differs, adapt ONLY this method.
        foreach (var (id, qty) in _s.Player.Inventory.Enumerate())
        {
            if (qty > 0) _items.Add((id, qty));
        }

        // stable order (optional): alphabetical by name
        _items.Sort((a, b) => string.Compare(_s.Items.NameOf(a.ItemId), _s.Items.NameOf(b.ItemId), StringComparison.OrdinalIgnoreCase));

        _focusIndex = Math.Clamp(_focusIndex, 0, Math.Max(0, _items.Count - 1));
        ClampScroll();
    }

    protected override void DrawUI(RenderContext rc)
    {
        if (_white == null) return;

        var sb = rc.SpriteBatch;
        sb.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.AlphaBlend);

        // Dim background
        sb.Draw(_white,
            new Rectangle(0, 0, PixelRenderer.InternalWidth, PixelRenderer.InternalHeight),
            Color.Black * 0.70f);

        // Main panel
        MenuRenderer.DrawPanel(sb, _white, _panel, new Color(18, 18, 34) * 0.98f);

        // Title
        _s.TitleFont.DrawShadow(sb, "INVENTORY", new Vector2(_panel.X + 118, _panel.Y + 14), Color.White, scale: 2);

        // List + Details + Hint
        MenuRenderer.DrawPanel(sb, _white, _panelList, new Color(12, 12, 20) * 0.98f);
        MenuRenderer.DrawPanel(sb, _white, _panelDetails, new Color(12, 12, 20) * 0.98f);
        MenuRenderer.DrawPanel(sb, _white, _panelHint, new Color(12, 12, 20) * 0.98f);

        DrawList(sb);
        DrawDetails(sb);
        DrawHint(sb);

        if (_toastT > 0f)
            DrawToast(sb);

        sb.End();
    }

    private void DrawList(SpriteBatch sb)
    {
        int rowH = _s.UiFont.LineHeight(1) + 6;
        int visible = VisibleRows();

        int x = _panelList.X + 8;
        int y = _panelList.Y + 8;

        if (_items.Count == 0)
        {
            _s.UiFont.Draw(sb, "(EMPTY)", new Vector2(x, y), Color.White * 0.6f, 1);
            return;
        }

        for (int i = 0; i < visible; i++)
        {
            int idx = _scroll + i;
            if (idx >= _items.Count) break;

            var (id, qty) = _items[idx];
            bool focused = (idx == _focusIndex);

            var r = new Rectangle(_panelList.X + 6, y - 2, _panelList.Width - 12, rowH);

            // draw row background + outline
            var fill = focused ? new Color(70, 70, 120) : new Color(40, 40, 70);
            sb.Draw(_white!, r, fill * 0.90f);
            MenuRenderer.DrawOutline(sb, _white!, r, 2, focused ? Color.White * 0.55f : Color.White * 0.20f);

            string name = _s.Items.NameOf(id).ToUpperInvariant();
            string left = name;
            string right = $"x{qty}";

            // trim name so qty fits
            int maxNameW = r.Width - 18 - _s.UiFont.Measure(right, 1).X;
            left = _s.UiFont.TrimToWidth(left, maxNameW, 1);

            _s.UiFont.Draw(sb, left, new Vector2(r.X + 8, r.Y + 5), Color.White * 0.90f, 1);
            _s.UiFont.Draw(sb, right, new Vector2(r.Right - 8 - _s.UiFont.Measure(right, 1).X, r.Y + 5), Color.White * 0.75f, 1);

            y += rowH;
        }
    }

    private void DrawDetails(SpriteBatch sb)
    {
        int x = _panelDetails.X + 10;
        int y = _panelDetails.Y + 10;

        if (_items.Count == 0)
        {
            _s.UiFont.Draw(sb, "NO ITEMS.", new Vector2(x, y), Color.White * 0.7f, 1);
            return;
        }

        var (id, qty) = _items[_focusIndex];

        string name = _s.Items.NameOf(id).ToUpperInvariant();
        _s.UiFont.Draw(sb, name, new Vector2(x, y), Color.White * 0.95f, 1);
        y += _s.UiFont.LineHeight(1) + 4;

        // If you have descriptions in ItemDb, show them. If not, this stays generic.
        string desc = _s.Items.DescOfOrFallback(id); // implement as safe fallback (see ItemDb note below)
        if (string.IsNullOrWhiteSpace(desc))
            desc = "A MYSTERIOUS ITEM.";

        // wordwrap-lite: split lines by trimming to width
        int maxW = _panelDetails.Width - 20;
        foreach (var line in Wrap(desc.ToUpperInvariant(), maxW))
        {
            _s.UiFont.Draw(sb, line, new Vector2(x, y), Color.White * 0.70f, 1);
            y += _s.UiFont.LineHeight(1);
            if (y > _panelDetails.Bottom - 12) break;
        }

        y += 4;
        _s.UiFont.Draw(sb, $"QTY: {qty}", new Vector2(x, _panelDetails.Bottom - 18), Color.White * 0.65f, 1);

        IEnumerable<string> Wrap(string text, int maxWidth)
        {
            // super simple wrap: keep trimming until it fits
            text = text.Replace("\n", " ");
            while (text.Length > 0)
            {
                var candidate = _s.UiFont.TrimToWidth(text, maxWidth, 1);

                // if TrimToWidth didn't shorten, consume all
                if (candidate.Length == text.Length)
                {
                    yield return candidate;
                    yield break;
                }

                // avoid chopping mid-word if possible
                int cut = candidate.LastIndexOf(' ');
                if (cut > 10)
                    candidate = candidate[..cut];

                yield return candidate.TrimEnd();

                text = text[candidate.Length..].TrimStart();
            }
        }
    }

    private void DrawHint(SpriteBatch sb)
    {
        string hint = "ENTER/A: USE    BACK/B: RETURN";
        _s.UiFont.Draw(sb, hint, new Vector2(_panelHint.X + 10, _panelHint.Y + 8), Color.White * 0.75f, 1);
    }

    private void DrawToast(SpriteBatch sb)
    {
        var r = new Rectangle(_panel.X + 60, _panel.Y + 34, _panel.Width - 120, 18);
        sb.Draw(_white!, r, new Color(10, 10, 18) * 0.88f);
        MenuRenderer.DrawOutline(sb, _white!, r, 1, Color.White * 0.25f);

        // centered
        var size = _s.UiFont.Measure(_toast, 1);
        var pos = new Vector2(r.X + (r.Width - size.X) / 2, r.Y + 2);
        _s.UiFont.Draw(sb, _toast, pos, Color.White * 0.92f, 1);
    }
}
