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

    private readonly Rectangle _panel = new(80, 60, 320, 150);
    private readonly Rectangle _btnStart = new(110, 105, 260, 32);
    private readonly Rectangle _btnQuit  = new(110, 145, 260, 32);

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

        // If _white is null, we can't draw much. But we can avoid crashing.
        if (_white == null)
        {
            sb.End();
            return;
        }

        // Background always safe now
        sb.Draw(_white, new Rectangle(0, 0, PixelRenderer.InternalWidth, PixelRenderer.InternalHeight),
            new Color(10, 12, 22));

        // If font is null, draw simple blocks and bail (prevents crash + tells you what's wrong)
        if (_s.Font == null)
        {
            // big panel
            sb.Draw(_white, new Rectangle(60, 70, 360, 120), new Color(25, 25, 45) * 0.95f);
            // "FONT NULL" blocks
            sb.Draw(_white, new Rectangle(80, 95, 120, 10), Color.Red * 0.8f);
            sb.Draw(_white, new Rectangle(80, 115, 180, 10), Color.Red * 0.6f);

            sb.End();
            return;
        }

        // Normal UI
        float t = (float)rc.GameTime.TotalGameTime.TotalSeconds;

        MenuRenderer.DrawPanel(sb, _white, _panel, new Color(18, 18, 34) * 0.98f);

        _s.UiFont.Draw(sb, "BEGINNER'S LUCK", new Vector2(_panel.X + 22, _panel.Y + 16), Color.White, scale: 2);

        MenuRenderer.DrawButton(sb, _white, _s.UiFont, _btnStart, "START",
            focused: _focus == 0, enabled: true, timeSeconds: t, fontScale: 2);

        MenuRenderer.DrawButton(sb, _white, _s.UiFont, _btnQuit, "QUIT",
            focused: _focus == 1, enabled: true, timeSeconds: t, fontScale: 2);

        sb.End();
    }

}
