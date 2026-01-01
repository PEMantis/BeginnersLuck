using BeginnersLuck.Engine;
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

    private readonly Rectangle _panel = new(110, 70, 260, 130);
    private readonly Rectangle _btnResume = new(130, 105, 220, 32);
    private readonly Rectangle _btnQuit = new(130, 145, 220, 32);

    private int _focus = 0;

    private readonly GameServices _s;

    private KeyboardState _prevKs;
    private GamePadState _prevPad;

    public PauseScene(GameServices s, KeyboardState seedKs, GamePadState seedPad)
    {
        _s = s;
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

        // Close pause (toggle): Esc / controller Start or Back
        if (Pressed(ks, Keys.Escape) || Pressed(pad, Buttons.Start) || Pressed(pad, Buttons.Back))
        {
            _s.Scenes.Pop();
            _prevKs = ks;
            _prevPad = pad;
            return;
        }

        // menu nav
        if (Pressed(ks, Keys.W) || Pressed(ks, Keys.Up) || Pressed(pad, Buttons.DPadUp)) _focus = 0;
        if (Pressed(ks, Keys.S) || Pressed(ks, Keys.Down) || Pressed(pad, Buttons.DPadDown)) _focus = 1;

        // select (Enter / A)
        if (Pressed(ks, Keys.Enter) || Pressed(ks, Keys.Space) || Pressed(pad, Buttons.A))
        {
            if (_focus == 0)
                _s.Scenes.Pop(); // Resume
            else
                _s.Scenes.Replace(new BootScene(_s, ks, pad)); // Menu
        }

        // keep prev updated
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

        // Dim the whole screen (virtual 480x270)
        sb.Draw(_white, new Rectangle(0, 0, PixelRenderer.InternalWidth, PixelRenderer.InternalHeight), Color.Black * 0.65f);

        MenuRenderer.DrawPanel(sb, _white, _panel, new Color(20, 20, 40) * 0.98f);

        _s.Font.Draw(sb, "PAUSED", new Vector2(_panel.X + 90, _panel.Y + 18), Color.White, scale: 2);

        MenuRenderer.DrawButton(sb, _white, _s.Font, _btnResume, "RESUME", focused: _focus == 0, enabled: true, timeSeconds: t, fontScale: 2);
        MenuRenderer.DrawButton(sb, _white, _s.Font, _btnQuit, "MENU", focused: _focus == 1, enabled: true, timeSeconds: t, fontScale: 2);

        _s.Font.Draw(sb, "ENTER: SELECT   ESC: BACK", new Vector2(_panel.X + 34, _panel.Bottom - 18), Color.White * 0.8f, scale: 1);

        sb.End();
    }
}
