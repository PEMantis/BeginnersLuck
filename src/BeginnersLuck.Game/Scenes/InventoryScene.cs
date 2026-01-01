using System;
using System.Linq;
using BeginnersLuck.Engine.Graphics;
using BeginnersLuck.Engine.Rendering;
using BeginnersLuck.Engine.Scenes;
using BeginnersLuck.Engine.UI;
using BeginnersLuck.Engine.Update;
using BeginnersLuck.Game.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BeginnersLuck.Game.Scenes;

public sealed class InventoryScene : SceneBase
{
    private readonly GameServices _s;

    private Texture2D? _white;

    private readonly Rectangle _panel = new(40, 24, 400, 220);
    private readonly Rectangle _listRect = new(56, 64, 368, 148);
    private readonly Rectangle _footerRect = new(56, 220, 368, 18);

    private int _selected;
    private int _scroll;

    private KeyboardState _prevKs;
    private GamePadState _prevPad;
    private bool _eatFirstUpdate = true;

    public InventoryScene(GameServices s, KeyboardState seedKs, GamePadState seedPad)
    {
        _s = s ?? throw new ArgumentNullException(nameof(s));
        _prevKs = seedKs;
        _prevPad = seedPad;
    }

    public override void Load(GraphicsDevice graphicsDevice, ContentManager content)
    {
        _white = new Texture2D(graphicsDevice, 1, 1);
        _white.SetData(new[] { Color.White });

        _selected = 0;
        _scroll = 0;
    }

    public override void Unload()
    {
        _white?.Dispose();
        _white = null;
    }

    public override void Update(UpdateContext uc)
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

        // Back/close (Esc/B/Back)
        if (Pressed(ks, Keys.Escape) || Pressed(ks, Keys.Back) || Pressed(pad, Buttons.B) || Pressed(pad, Buttons.Back))
        {
            _s.Scenes.Pop();
            _prevKs = ks;
            _prevPad = pad;
            return;
        }

        // Build a stable list view each frame (fine for now; later cache if you want)
        var items = _s.Player.Inventory.Counts
            .Where(kv => kv.Value > 0)
            .OrderBy(kv => _s.Items.NameOf(kv.Key))
            .Select(kv => (Id: kv.Key, Name: _s.Items.NameOf(kv.Key), Qty: kv.Value))
            .ToArray();

        if (items.Length == 0)
        {
            _selected = 0;
            _scroll = 0;
        }
        else
        {
            int visibleRows = Math.Max(1, _listRect.Height / 12); // 8x8 font, scale 1, line step 12
            _selected = Math.Clamp(_selected, 0, items.Length - 1);

            // Move selection
            if (PressedUp(ks, pad)) _selected--;
            if (PressedDown(ks, pad)) _selected++;

            _selected = Math.Clamp(_selected, 0, items.Length - 1);

            // Keep selected visible
            if (_selected < _scroll) _scroll = _selected;
            if (_selected >= _scroll + visibleRows) _scroll = _selected - visibleRows + 1;

            _scroll = Math.Clamp(_scroll, 0, Math.Max(0, items.Length - visibleRows));
        }

        _prevKs = ks;
        _prevPad = pad;
    }

    protected override void DrawUI(RenderContext rc)
    {
        if (_white == null) return;

        var sb = rc.SpriteBatch;

        sb.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.AlphaBlend);

        // Dim background
        sb.Draw(_white, new Rectangle(0, 0, PixelRenderer.InternalWidth, PixelRenderer.InternalHeight), Color.Black * 0.70f);

        // Panel
        MenuRenderer.DrawPanel(sb, _white, _panel, new Color(18, 18, 34) * 0.98f);

        // Title
        _s.Font.Draw(sb, "INVENTORY", new Vector2(_panel.X + 16, _panel.Y + 14), Color.White * 0.92f, scale: 2);

        // List background (subtle)
        sb.Draw(_white, _listRect, new Color(10, 10, 18) * 0.35f);

        // Items
        var items = _s.Player.Inventory.Counts
            .Where(kv => kv.Value > 0)
            .OrderBy(kv => _s.Items.NameOf(kv.Key))
            .Select(kv => (Id: kv.Key, Name: _s.Items.NameOf(kv.Key), Qty: kv.Value))
            .ToArray();

        if (items.Length == 0)
        {
            _s.Font.Draw(sb, "(EMPTY)", new Vector2(_listRect.X + 8, _listRect.Y + 8), Color.White * 0.65f, 1);
        }
        else
        {
            int rowH = 12;
            int visibleRows = Math.Max(1, _listRect.Height / rowH);

            int start = _scroll;
            int end = Math.Min(items.Length, start + visibleRows);

            int y = _listRect.Y + 6;

            for (int i = start; i < end; i++)
            {
                bool sel = (i == _selected);

                if (sel)
                {
                    // selection bar
                    sb.Draw(_white, new Rectangle(_listRect.X + 2, y - 2, _listRect.Width - 4, rowH), new Color(80, 140, 255) * 0.18f);
                    sb.Draw(_white, new Rectangle(_listRect.X + 2, y - 2, 2, rowH), new Color(140, 200, 255) * 0.55f);
                }

                var (id, name, qty) = items[i];
                _s.Font.Draw(sb, name.ToUpperInvariant(), new Vector2(_listRect.X + 10, y), Color.White * 0.85f, 1);

                // Right-aligned quantity
                var qtyText = $"x{qty}";
                // crude right-align: assume monospace 8px
                int qtyW = qtyText.Length * 8;
                _s.Font.Draw(sb, qtyText.ToUpperInvariant(), new Vector2(_listRect.Right - 12 - qtyW, y), Color.White * 0.75f, 1);

                y += rowH;
            }

            // Scroll hint
            if (items.Length > visibleRows)
            {
                var hint = $"{_selected + 1}/{items.Length}";
                _s.Font.Draw(sb, hint, new Vector2(_listRect.Right - 8 - hint.Length * 8, _listRect.Y - 14), Color.White * 0.55f, 1);
            }
        }

        // Footer
        sb.Draw(_white, _footerRect, new Color(10, 10, 18) * 0.35f);
        _s.Font.Draw(sb, "ESC/BACK: RETURN", new Vector2(_footerRect.X + 8, _footerRect.Y + 5), Color.White * 0.65f, 1);

        sb.End();
    }

    // --- Input helpers ---
    private bool Pressed(KeyboardState ks, Keys k) => ks.IsKeyDown(k) && !_prevKs.IsKeyDown(k);
    private bool Pressed(GamePadState pad, Buttons b) => pad.IsButtonDown(b) && !_prevPad.IsButtonDown(b);

    private bool PressedUp(KeyboardState ks, GamePadState pad)
        => Pressed(ks, Keys.Up) || Pressed(ks, Keys.W) || Pressed(pad, Buttons.DPadUp);

    private bool PressedDown(KeyboardState ks, GamePadState pad)
        => Pressed(ks, Keys.Down) || Pressed(ks, Keys.S) || Pressed(pad, Buttons.DPadDown);
}
