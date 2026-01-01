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

public sealed class PauseScene : SceneBase
{
    private Texture2D? _white;

    private readonly Rectangle _panel        = new(110, 58, 260, 154);
    private readonly Rectangle _btnResume    = new(130, 92,  220, 32);
    private readonly Rectangle _btnInventory = new(130, 132, 220, 32);
    private readonly Rectangle _btnMenu      = new(130, 172, 220, 32);

    // 0 Resume, 1 Inventory, 2 Menu
    private int _focus;

    private readonly GameServices _s;

    private KeyboardState _prevKs;
    private GamePadState _prevPad;
    private bool _eatFirstUpdate = true;

    public PauseScene(GameServices s, KeyboardState seedKs, GamePadState seedPad)
    {
        _s = s ?? throw new ArgumentNullException(nameof(s));
        _focus = 0;
        _prevKs = seedKs;
        _prevPad = seedPad;
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

        // Prevent "press-through" from the key that opened pause
        if (_eatFirstUpdate)
        {
            _eatFirstUpdate = false;
            _prevKs = ks;
            _prevPad = pad;
            return;
        }

        // Back/close pause: Backspace (keyboard) OR B/Back (controller)
        if (Pressed(ks, Keys.Back) || Pressed(pad, Buttons.B) || Pressed(pad, Buttons.Back))
        {
            _s.Scenes.Pop();
            _prevKs = ks;
            _prevPad = pad;
            return;
        }

        // Navigate (Up/Down, W/S, DPad)
        if (Pressed(ks, Keys.Up) || Pressed(ks, Keys.W) || Pressed(pad, Buttons.DPadUp))
            _focus = Math.Max(0, _focus - 1);

        if (Pressed(ks, Keys.Down) || Pressed(ks, Keys.S) || Pressed(pad, Buttons.DPadDown))
            _focus = Math.Min(2, _focus + 1);

        // Select (Enter/Space/A)
        if (Pressed(ks, Keys.Enter) || Pressed(ks, Keys.Space) || Pressed(pad, Buttons.A))
        {
            if (_focus == 0)
            {
                _s.Scenes.Pop(); // Resume
            }
            else if (_focus == 1)
            {
                _s.Scenes.Push(new MenuHubScene(_s, ks, pad, startTab: 0));
            }
            else
            {
                _s.Scenes.Replace(new BootScene(_s, ks, pad));
            }

            _prevKs = ks;
            _prevPad = pad;
            return;
        }

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

        // Dim screen
        sb.Draw(_white,
            new Rectangle(0, 0, PixelRenderer.InternalWidth, PixelRenderer.InternalHeight),
            Color.Black * 0.65f);

        // Panel
        MenuRenderer.DrawPanel(sb, _white, _panel, new Color(20, 20, 40) * 0.98f);

        _s.TitleFont.DrawShadow(sb, "PAUSED", new Vector2(_panel.X + 90, _panel.Y + 12), Color.White, scale: 2);

        MenuRenderer.DrawButton(sb, _white, _s.UiFont, _btnResume, "RESUME",
            focused: _focus == 0, enabled: true, timeSeconds: t, fontScale: 2);

        MenuRenderer.DrawButton(sb, _white, _s.UiFont, _btnInventory, "INVENTORY",
            focused: _focus == 1, enabled: true, timeSeconds: t, fontScale: 2);

        MenuRenderer.DrawButton(sb, _white, _s.UiFont, _btnMenu, "MENU",
            focused: _focus == 2, enabled: true, timeSeconds: t, fontScale: 2);

        _s.TitleFont.Draw(sb, "ENTER/A: SELECT   BACK/B: RETURN",
            new Vector2(_panel.X + 30, _panel.Bottom - 16),
            Color.White * 0.8f, scale: 1);

        sb.End();
    }
}
