using System;
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

public sealed class BootScene : SceneBase
{
    private readonly GameServices _s;
    private Texture2D? _white;
    private int _focus = 0;
    private KeyboardState _prevKs;
    private GamePadState _prevPad;
    private bool _eatFirstUpdate = true;

    // Normal entry (first boot)
    public BootScene(GameServices s)
    {
        _s = s ?? throw new ArgumentNullException(nameof(s), "BootScene requires GameServices (was null).");
        _prevKs = default;
        _prevPad = default;
    }

    // Return-to-menu entry (seed input so arrows work immediately)
    public BootScene(GameServices s, KeyboardState seedKs, GamePadState seedPad)
    {
        _s = s ?? throw new ArgumentNullException(nameof(s), "BootScene requires GameServices (was null).");
        _prevKs = seedKs;
        _prevPad = seedPad;
        _eatFirstUpdate = true;
    }

    public override void Load(GraphicsDevice graphicsDevice, ContentManager content)
    {
        _white = new Texture2D(graphicsDevice, 1, 1);
        _white.SetData(new[] { Color.White });
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

        // Prevent immediate “press-through” when returning from Pause
        if (_eatFirstUpdate)
        {
            _eatFirstUpdate = false;
            _prevKs = ks;
            _prevPad = pad;
            return;
        }

        // Navigation
        if (Pressed(ks, Keys.Up) || Pressed(ks, Keys.W) || Pressed(pad, Buttons.DPadUp))
            _focus = 0;

        if (Pressed(ks, Keys.Down) || Pressed(ks, Keys.S) || Pressed(pad, Buttons.DPadDown))
            _focus = 1;

        // Select
        if (Pressed(ks, Keys.Enter) || Pressed(ks, Keys.Space) || Pressed(pad, Buttons.A))
        {
            if (_focus == 0)
            {
                // Start game
                _s.Scenes.Replace(new WorldMapScene(_s));
                uc.Actions.ConsumeAll();
                _prevKs = ks;
                _prevPad = pad;
                return;
            }
            else
            {
                // Quit
                // If you prefer: _s.Scenes.Replace(new QuitConfirmScene(_s, ks, pad));
                System.Environment.Exit(0);
            }
        }

        _prevKs = ks;
        _prevPad = pad;
    }

    private bool Pressed(KeyboardState ks, Keys k) => ks.IsKeyDown(k) && !_prevKs.IsKeyDown(k);
    private bool Pressed(GamePadState pad, Buttons b) => pad.IsButtonDown(b) && !_prevPad.IsButtonDown(b);

    protected override void DrawUI(RenderContext rc)
    {
        var sb = rc.SpriteBatch;

        sb.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.AlphaBlend);

        if (_white == null)
        {
            sb.End();
            return;
        }

        // Background
        sb.Draw(_white,
            new Rectangle(0, 0, PixelRenderer.InternalWidth, PixelRenderer.InternalHeight),
            new Color(10, 12, 22));

        // Basic guard (Font should never be null, but still)
        if (_s.UiFont == null || _s.TitleFont == null || _s.ButtonFont == null)
        {
            sb.End();
            return;
        }

        float t = (float)rc.GameTime.TotalGameTime.TotalSeconds;

        // ===== Layout =====
        // Use a safe area so this scales with your internal resolution.
        var screen = new Rectangle(0, 0, PixelRenderer.InternalWidth, PixelRenderer.InternalHeight);
        var safe = UiLayout.Inset(screen, 24);

        // Panel size derived from content instead of hard-coded.
        // (Tweak these 3 numbers once and everything stays aligned.)
        int panelW = 360;
        int panelH = 170;

        var panel = UiLayout.Centered(safe, panelW, panelH);

        // Title line
        var titlePos = new Vector2(panel.X + 18, panel.Y + 14);

        // Buttons (same width, stacked)
        int btnW = panelW - 60;
        int btnH = 30;
        int btnX = panel.X + (panelW - btnW) / 2;
        int btnY0 = panel.Y + 62;
        int btnGap = 10;

        var btnStart = new Rectangle(btnX, btnY0, btnW, btnH);
        var btnQuit = new Rectangle(btnX, btnY0 + btnH + btnGap, btnW, btnH);

        // ===== Draw =====
        MenuRenderer.DrawPanel(sb, _white, panel, new Color(18, 18, 34) * 0.98f);

        // Title
        var title = "BEGINNER'S LUCK";
        var titleSize = _s.TitleFont.Measure(title, 1);
        var titleX = panel.X + (panel.Width - titleSize.X) / 2;
        var titleY = panel.Y + 14;
        _s.TitleFont.DrawString(sb, title, new Vector2(titleX, titleY), Color.White, 1);

        // Buttons should also be scale 1 with ButtonFont.
        MenuRenderer.DrawButton(sb, _white, _s.ButtonFont, btnStart, "START",
            focused: _focus == 0, enabled: true, timeSeconds: t, fontScale: 1, contentPadX: 24);

        MenuRenderer.DrawButton(sb, _white, _s.ButtonFont, btnQuit, "QUIT",
            focused: _focus == 1, enabled: true, timeSeconds: t, fontScale: 1, contentPadX: 24);

        sb.End();
    }

}
