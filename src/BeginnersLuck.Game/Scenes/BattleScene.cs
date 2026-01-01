using System;
using BeginnersLuck.Engine.Graphics;
using BeginnersLuck.Engine.Rendering;
using BeginnersLuck.Engine.Scenes;
using BeginnersLuck.Engine.UI;
using BeginnersLuck.Engine.Update;
using BeginnersLuck.Game.Encounters;
using BeginnersLuck.Game.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BeginnersLuck.Game.Scenes;

public sealed class BattleScene : SceneBase
{
    private readonly GameServices _s;
    private readonly EncounterDef _encounter;

    private Texture2D? _white;

    private KeyboardState _prevKs;
    private GamePadState _prevPad;
    private bool _eatFirstUpdate = true;

    private readonly Rectangle _panel = new(40, 40, 400, 190);

    public BattleScene(GameServices s, EncounterDef encounter, KeyboardState seedKs, GamePadState seedPad)
    {
        _s = s;
        _encounter = encounter;
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

        // Prevent the “push + immediate pop” from the same button press
        if (_eatFirstUpdate)
        {
            _eatFirstUpdate = false;
            _prevKs = ks;
            _prevPad = pad;
            return;
        }

        // Leave battle: Esc/Backspace or controller B/Back
        if (Pressed(ks, Keys.Enter) || Pressed(ks, Keys.Space) ||
            Pressed(pad, Buttons.A))
        {
            _s.Scenes.Pop();
            return;
        }

        _prevKs = ks;
        _prevPad = pad;
    }

    private bool Pressed(KeyboardState ks, Keys k) => ks.IsKeyDown(k) && !_prevKs.IsKeyDown(k);
    private bool Pressed(GamePadState pad, Buttons b) => pad.IsButtonDown(b) && !_prevPad.IsButtonDown(b);

    protected override void DrawWorld(RenderContext rc)
    {
        // battle is UI-only for now
    }

    protected override void DrawUI(RenderContext rc)
    {
        if (_white == null) return;

        var sb = rc.SpriteBatch;
        float t = (float)rc.GameTime.TotalGameTime.TotalSeconds;

        sb.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.AlphaBlend);

        // Dim background
        sb.Draw(_white, new Rectangle(0, 0, PixelRenderer.InternalWidth, PixelRenderer.InternalHeight),
            Color.Black * 0.75f);

        // Panel
        MenuRenderer.DrawPanel(sb, _white, _panel, new Color(18, 18, 34) * 0.98f);

        // Title
        _s.Font.Draw(sb, "BATTLE!", new Vector2(_panel.X + 14, _panel.Y + 12), Color.White, scale: 2);
        _s.Font.Draw(sb, _encounter.Name.ToUpperInvariant(), new Vector2(_panel.X + 14, _panel.Y + 40),
            Color.White * 0.9f, scale: 1);

        // Enemy list
        int y = _panel.Y + 70;
        if (_encounter.Enemies.Length == 0)
        {
            _s.Font.Draw(sb, "NO ENEMIES (DEV)", new Vector2(_panel.X + 14, y), Color.White * 0.75f, scale: 1);
        }
        else
        {
            for (int i = 0; i < _encounter.Enemies.Length; i++)
            {
                var e = _encounter.Enemies[i];
                _s.Font.Draw(sb, $"> {e.Name}  HP:{e.Hp}", new Vector2(_panel.X + 14, y), Color.White * 0.85f, 1);
                y += 14;
            }
        }

        // Footer hint
        var hint = "ESC/B: RUN";
        var hintR = new Rectangle(_panel.X, _panel.Bottom - 24, _panel.Width, 20);
        DrawTextCentered(sb, _s.Font, hint, hintR, Color.White * (0.75f + 0.2f * (float)Math.Sin(t * 4f)), 1);

        sb.End();
    }

    // Small local helper to keep BattleScene self-contained
    private static void DrawTextCentered(SpriteBatch sb, BitmapFont font, string text, Rectangle r, Color color, int scale)
    {
        var size = font.MeasureFixed8x8(text, scale);
        var pos = new Vector2(
            r.X + (r.Width - size.X) * 0.5f,
            r.Y + (r.Height - size.Y) * 0.5f);
        font.Draw(sb, text, pos, color, scale);
    }
}
