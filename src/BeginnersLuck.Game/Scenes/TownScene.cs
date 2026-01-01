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

public sealed class TownScene : SceneBase
{
    private readonly GameServices _s;
    private readonly string _townId;

    private Texture2D? _white;

    private KeyboardState _prevKs;
    private GamePadState _prevPad;

    private readonly Rectangle _panel = new(70, 70, 340, 130);

    public TownScene(GameServices s, string townId, KeyboardState seedKs, GamePadState seedPad)
    {
        _s = s;
        _townId = string.IsNullOrWhiteSpace(townId) ? "unknown_town" : townId;
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

        // Leave town: Esc / B / Back
        if (Pressed(ks, Keys.Escape) || Pressed(pad, Buttons.B) || Pressed(pad, Buttons.Back))
        {
            if (!_s.Fade.Active)
            {
                _s.Fade.Start(0.20f, () => _s.Scenes.Pop());
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

        sb.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.AlphaBlend);

        // Dim background
        sb.Draw(_white, new Rectangle(0, 0, PixelRenderer.InternalWidth, PixelRenderer.InternalHeight), Color.Black * 0.6f);

        MenuRenderer.DrawPanel(sb, _white, _panel, new Color(20, 20, 40) * 0.98f);

        _s.TitleFont.Draw(sb, "TOWN", new Vector2(_panel.X + 24, _panel.Y + 20), Color.White, scale: 2);
        _s.UiFont.Draw(sb, _townId.ToUpperInvariant(), new Vector2(_panel.X + 24, _panel.Y + 52), Color.White * 0.9f, scale: 1);
        _s.TitleFont.Draw(sb, "ESC/B: LEAVE", new Vector2(_panel.X + 24, _panel.Bottom - 22), Color.White * 0.8f, scale: 1);

        sb.End();
    }
}
