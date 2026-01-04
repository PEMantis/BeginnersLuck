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

public sealed class TownScene : SceneBase
{
    private readonly GameServices _s;
    private readonly Point _worldTile;

    private KeyboardState _prevKs;
    private GamePadState _prevPad;

    private int _sel;

    private static readonly string[] Items =
    {
        "Shop",
        "Rest",
        "Leave"
    };

    public TownScene(GameServices s, Point worldTile)
    {
        _s = s ?? throw new ArgumentNullException(nameof(s));
        _worldTile = worldTile;
    }

    public override void Load(GraphicsDevice graphicsDevice, ContentManager content)
    {
        _sel = 0;
    }

    public override void Unload()
    {
       
    }
    
    public override void Update(UpdateContext uc)
    {
        var ks = Keyboard.GetState();
        var pad = GamePad.GetState(PlayerIndex.One);

        // Back
        if (Pressed(pad, Buttons.B) || Pressed(ks, Keys.Escape))
        {
            _s.Scenes.Pop();
            _prevKs = ks;
            _prevPad = pad;
            return;
        }

        // Nav
        if (Pressed(pad, Buttons.DPadUp) || Pressed(ks, Keys.Up) || Pressed(ks, Keys.W))
            _sel = (_sel - 1 + Items.Length) % Items.Length;

        if (Pressed(pad, Buttons.DPadDown) || Pressed(ks, Keys.Down) || Pressed(ks, Keys.S))
            _sel = (_sel + 1) % Items.Length;

        // Confirm
        if (Pressed(pad, Buttons.A) || Pressed(ks, Keys.Enter) || Pressed(ks, Keys.Space))
        {
            switch (_sel)
            {
                case 0:
                    _s.Toasts.Push("Shop: coming soon.", 0.8f);
                    break;

                case 1:
                    _s.Toasts.Push("Rested.", 0.6f);
                    break;

                case 2:
                    _s.Scenes.Pop();
                    break;
            }
        }

        _prevKs = ks;
        _prevPad = pad;
    }

    protected override void DrawUI(RenderContext rc)
    {
        var sb = rc.SpriteBatch;

        sb.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.AlphaBlend);

        // Dim overlay
        sb.Draw(_s.PixelWhite,
            new Rectangle(0, 0, PixelRenderer.InternalWidth, PixelRenderer.InternalHeight),
            Color.Black * 0.55f);

        // Panel
        int panelW = 260;
        int panelH = 140;
        var panel = new Rectangle(
            (PixelRenderer.InternalWidth - panelW) / 2,
            (PixelRenderer.InternalHeight - panelH) / 2,
            panelW,
            panelH);

        sb.Draw(_s.PixelWhite, panel, new Color(10, 10, 18) * 0.92f);
        DrawBorder(sb, panel, Color.White * 0.25f);

        _s.UiFont.Draw(sb, $"Town ({_worldTile.X},{_worldTile.Y})",
            new Vector2(panel.X + 12, panel.Y + 10),
            Color.White * 0.95f, scale: 1);

        _s.UiFont.Draw(sb, "D-Pad: select   A: confirm   B: back",
            new Vector2(panel.X + 12, panel.Y + 26),
            Color.White * 0.7f, scale: 1);

        int y = panel.Y + 50;
        for (int i = 0; i < Items.Length; i++)
        {
            bool focused = i == _sel;

            var itemR = new Rectangle(panel.X + 12, y, panelW - 24, 22);
            sb.Draw(_s.PixelWhite, itemR,
                focused ? new Color(70, 70, 120) * 0.9f
                        : new Color(25, 25, 40) * 0.9f);

            DrawBorder(sb, itemR, Color.White * (focused ? 0.35f : 0.2f));

            _s.UiFont.Draw(sb, Items[i],
                new Vector2(itemR.X + 10, itemR.Y + 6),
                Color.White * (focused ? 0.95f : 0.8f),
                scale: 1);

            y += 26;
        }

        sb.End();
    }

    private void DrawBorder(SpriteBatch sb, Rectangle r, Color c)
    {
        sb.Draw(_s.PixelWhite, new Rectangle(r.X, r.Y, r.Width, 1), c);
        sb.Draw(_s.PixelWhite, new Rectangle(r.X, r.Y + r.Height - 1, r.Width, 1), c);
        sb.Draw(_s.PixelWhite, new Rectangle(r.X, r.Y, 1, r.Height), c);
        sb.Draw(_s.PixelWhite, new Rectangle(r.X + r.Width - 1, r.Y, 1, r.Height), c);
    }

    private bool Pressed(KeyboardState ks, Keys k) => ks.IsKeyDown(k) && !_prevKs.IsKeyDown(k);
    private bool Pressed(GamePadState pad, Buttons b) => pad.IsButtonDown(b) && !_prevPad.IsButtonDown(b);
}
