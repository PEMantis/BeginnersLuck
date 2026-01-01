using System;
using System.Collections.Generic;
using BeginnersLuck.Engine.Graphics;
using BeginnersLuck.Engine.Rendering;
using BeginnersLuck.Engine.Scenes;
using BeginnersLuck.Engine.UI;
using BeginnersLuck.Engine.Update;
using BeginnersLuck.Game.Menu;
using BeginnersLuck.Game.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BeginnersLuck.Game.Scenes;

public sealed class MenuHubScene : SceneBase
{
    private readonly GameServices _s;

    private Texture2D? _white;

    private readonly Rectangle _panel = new(24, 18, 432, 234);
    private readonly Rectangle _tabsRect = new(34, 48, 140, 194);
    private readonly Rectangle _contentRect = new(184, 48, 262, 194);

    private readonly List<IMenuPage> _pages = new();

    private int _tabIndex;

    private KeyboardState _prevKs;
    private GamePadState _prevPad;
    private bool _eatFirstUpdate = true;

    public MenuHubScene(GameServices s, KeyboardState seedKs, GamePadState seedPad, int startTab = 0)
    {
        _s = s ?? throw new ArgumentNullException(nameof(s));
        _prevKs = seedKs;
        _prevPad = seedPad;
        _tabIndex = startTab;
    }

    public override void Load(GraphicsDevice graphicsDevice, ContentManager content)
    {
        _white = new Texture2D(graphicsDevice, 1, 1);
        _white.SetData(new[] { Color.White });

        // Register pages (order matters)
        _pages.Clear();
        _pages.Add(new InventoryPage());
        _pages.Add(new StubPage("CHARACTER", "Stats + equipment later."));
        _pages.Add(new StubPage("SKILLS", "Active + passive skills later."));
        _pages.Add(new StubPage("MAGIC", "Spells, mana, spellbook later."));
        _pages.Add(new StubPage("JOURNAL", "Quests + notes later."));

        _tabIndex = Math.Clamp(_tabIndex, 0, _pages.Count - 1);

        _pages[_tabIndex].OnEnter(_s);
    }

    public override void Unload()
    {
        if (_pages.Count > 0 && _tabIndex >= 0 && _tabIndex < _pages.Count)
            _pages[_tabIndex].OnExit(_s);

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

        // Close hub: Backspace (kbd) or B/Back (pad)
        if (Pressed(ks, Keys.Back) || Pressed(pad, Buttons.B) || Pressed(pad, Buttons.Back))
        {
            _s.Scenes.Pop();
            _prevKs = ks;
            _prevPad = pad;
            return;
        }

        int prevTab = _tabIndex;

        // Tab nav
        if (Pressed(ks, Keys.Up) || Pressed(ks, Keys.W) || Pressed(pad, Buttons.DPadUp))
            _tabIndex = Math.Max(0, _tabIndex - 1);

        if (Pressed(ks, Keys.Down) || Pressed(ks, Keys.S) || Pressed(pad, Buttons.DPadDown))
            _tabIndex = Math.Min(_pages.Count - 1, _tabIndex + 1);

        if (_tabIndex != prevTab)
        {
            _pages[prevTab].OnExit(_s);
            _pages[_tabIndex].OnEnter(_s);
        }

        float dt = (float)uc.GameTime.ElapsedGameTime.TotalSeconds;
        _pages[_tabIndex].Update(_s, dt);

        _prevKs = ks;
        _prevPad = pad;
    }

    private bool Pressed(KeyboardState ks, Keys k) => ks.IsKeyDown(k) && !_prevKs.IsKeyDown(k);
    private bool Pressed(GamePadState pad, Buttons b) => pad.IsButtonDown(b) && !_prevPad.IsButtonDown(b);

    protected override void DrawUI(RenderContext rc)
    {
        if (_white == null) return;

        var sb = rc.SpriteBatch;
        float t = (float)rc.GameTime.TotalGameTime.TotalSeconds;

        sb.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.AlphaBlend);

        // Dim
        sb.Draw(_white, new Rectangle(0, 0, PixelRenderer.InternalWidth, PixelRenderer.InternalHeight), Color.Black * 0.70f);

        // Main panel
        MenuRenderer.DrawPanel(sb, _white, _panel, new Color(18, 18, 34) * 0.98f);

        // Tabs background + content background
        sb.Draw(_white, _tabsRect, new Color(10, 10, 18) * 0.35f);
        sb.Draw(_white, _contentRect, new Color(10, 10, 18) * 0.25f);

        // Title top
        _s.Font.Draw(sb, "MENU", new Vector2(_panel.X + 18, _panel.Y + 10), Color.White * 0.9f, 2);

        // Tabs list
        int rowH = 20;
        int y = _tabsRect.Y + 10;

        for (int i = 0; i < _pages.Count; i++)
        {
            bool sel = i == _tabIndex;

            if (sel)
            {
                sb.Draw(_white, new Rectangle(_tabsRect.X + 4, y - 2, _tabsRect.Width - 8, 18), new Color(80, 140, 255) * 0.18f);
                sb.Draw(_white, new Rectangle(_tabsRect.X + 4, y - 2, 2, 18), new Color(140, 200, 255) * 0.55f);
            }

            _s.Font.Draw(sb, _pages[i].Title, new Vector2(_tabsRect.X + 12, y), Color.White * (sel ? 0.95f : 0.70f), 1);
            y += rowH;
        }

        // Page header
        _s.Font.Draw(sb, _pages[_tabIndex].Title, new Vector2(_contentRect.X + 12, _contentRect.Y + 10), Color.White * 0.9f, 2);

        // Page content area (leave a header margin)
        var inner = new Rectangle(_contentRect.X, _contentRect.Y + 26, _contentRect.Width, _contentRect.Height - 26);
        _pages[_tabIndex].Draw(_s, sb, inner, t);

        // Footer hints
        _s.Font.Draw(sb, "UP/DOWN: TAB   BACK/B: CLOSE",
            new Vector2(_panel.X + 24, _panel.Bottom - 16),
            Color.White * 0.65f, 1);

        sb.End();
    }
}
