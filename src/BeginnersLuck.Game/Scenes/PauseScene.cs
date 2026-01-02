using System;
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

public sealed class PauseScene : SceneBase
{
    private Texture2D? _white;
    private readonly GameServices _s;

    private Rectangle _screen;
    private Rectangle _panel;

    private Rectangle _btnResume;
    private Rectangle _btnMenu;

    private int _focus; // 0 resume, 1 menu
    private bool _consumeOnFirstUpdate = true;

    private const string LeftHint  = "ENTER/A: SELECT";
    private const string RightHint = "BACK/B: RETURN";

    public PauseScene(GameServices s)
    {
        _s = s ?? throw new ArgumentNullException(nameof(s));
        _focus = 0;
    }

    public override void Load(GraphicsDevice graphicsDevice, ContentManager content)
    {
        _white = new Texture2D(graphicsDevice, 1, 1);
        _white.SetData(new[] { Color.White });

        _consumeOnFirstUpdate = true;
        ComputeLayout();
    }

    public override void Unload()
    {
        _white?.Dispose();
        _white = null;
    }

    public override void Update(UpdateContext uc)
    {
        if (_consumeOnFirstUpdate)
        {
            _consumeOnFirstUpdate = false;
            uc.Actions.ConsumeAll();
            return;
        }

        if (uc.Actions.Pressed(uc.Input, GameAction.Cancel))
        {
            _s.Scenes.Pop();
            uc.Actions.ConsumeAll();
            return;
        }

        if (uc.Actions.Pressed(uc.Input, GameAction.MoveUp))
            _focus = Math.Max(0, _focus - 1);

        if (uc.Actions.Pressed(uc.Input, GameAction.MoveDown))
            _focus = Math.Min(1, _focus + 1);

        if (uc.Actions.Pressed(uc.Input, GameAction.Confirm))
        {
            if (_focus == 0)
            {
                _s.Scenes.Pop();
                uc.Actions.ConsumeAll();
                return;
            }

            _s.Scenes.Replace(new BootScene(_s));
            uc.Actions.ConsumeAll();
        }
    }

    protected override void DrawUI(RenderContext rc)
    {
        if (_white == null) return;

        // Keep layout synced if internal resolution changes
        if (_screen.Width != PixelRenderer.InternalWidth || _screen.Height != PixelRenderer.InternalHeight)
            ComputeLayout();

        var sb = rc.SpriteBatch;
        float t = (float)rc.GameTime.TotalGameTime.TotalSeconds;

        sb.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.AlphaBlend);

        sb.Draw(_white, _screen, Color.Black * 0.65f);

        MenuRenderer.DrawPanel(sb, _white, _panel, new Color(20, 20, 40) * 0.98f);

        // Title centered in top band
        DrawCenteredInRect(sb, _s.TitleFont, "PAUSED", TitleBandRect(), Color.White, 2);

        // Buttons
        MenuRenderer.DrawButton(sb, _white, _s.UiFont, _btnResume, "RESUME",
            focused: _focus == 0, enabled: true, timeSeconds: t, fontScale: 2);

        MenuRenderer.DrawButton(sb, _white, _s.UiFont, _btnMenu, "MENU",
            focused: _focus == 1, enabled: true, timeSeconds: t, fontScale: 2);

        // Footer hint (reserved band so it can't overlap)
        MenuRenderer.DrawFooterHint(
            sb,
            _s.UiFont,
            _panel,
            LeftHint,
            RightHint,
            Color.White * 0.80f,
            scale: 1,
            padding: 10);

        sb.End();
    }

    private void ComputeLayout()
    {
        int w = PixelRenderer.InternalWidth;
        int h = PixelRenderer.InternalHeight;

        _screen = new Rectangle(0, 0, w, h);

        // Panel size
        int panelW = Math.Max(280, (int)(w * 0.48f));

        // Compute height from contents so footer always fits:
        int titleBand = 56;
        int footerBand = 28;
        int padTop = 10;
        int padBottom = 10;

        int btnH = 34;
        int btnGap = 12;
        int buttonsBlock = btnH + btnGap + btnH;

        int panelH = padTop + titleBand + buttonsBlock + footerBand + padBottom;
        panelH = Math.Max(panelH, 150);

        _panel = new Rectangle(
            (w - panelW) / 2,
            (h - panelH) / 2,
            panelW,
            panelH);

        int padX = 20;

        int btnW = _panel.Width - padX * 2;
        int btnX = _panel.X + padX;

        int btnY = _panel.Y + padTop + titleBand;

        _btnResume = new Rectangle(btnX, btnY, btnW, btnH);
        _btnMenu   = new Rectangle(btnX, btnY + btnH + btnGap, btnW, btnH);
    }

    private Rectangle TitleBandRect()
    {
        // A band inside the panel for the title to live in
        return new Rectangle(_panel.X + 10, _panel.Y + 10, _panel.Width - 20, 44);
    }

    private static void DrawCenteredInRect(SpriteBatch sb, IFont font, string text, Rectangle r, Color color, int scale)
    {
        var size = font.Measure(text, scale);
        int x = r.X + (r.Width - size.X) / 2;
        int y = r.Y + (r.Height - font.LineHeight(scale)) / 2;
        font.Draw(sb, text, new Vector2(x, y), color, scale);
    }
}
