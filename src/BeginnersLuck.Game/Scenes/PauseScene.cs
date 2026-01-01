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

    // Panel
    private readonly Rectangle _panel = new(110, 58, 260, 154);

    // Computed buttons
    private Rectangle _btnResume;
    private Rectangle _btnInventory;
    private Rectangle _btnMenu;

    private int _focus;
    private readonly GameServices _s;

    private KeyboardState _prevKs;
    private GamePadState _prevPad;
    private bool _eatFirstUpdate = true;

    // Layout tuning
    private const int PadX = 20;
    private const int TitleTop = 10;
    private const int TitleScale = 2;
    private const int FooterScale = 1;
    private const int FooterPad = 8;

    // Desired button sizing (will shrink if panel is too short)
    private const int WantButtonH = 32;
    private const int WantGap = 8;

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

        RecomputeLayout();
    }

    private void RecomputeLayout()
    {
        // Measure title + footer using your font metrics (so it matches reality)
        int titleH = _s.TitleFont.LineHeight(TitleScale);
        int footerH = _s.TitleFont.LineHeight(FooterScale);

        // Reserve bands
        int topBand = TitleTop + titleH + 8; // title area + a little breathing room
        int bottomBand = FooterPad + footerH + FooterPad; // footer padding

        // Usable button region inside panel
        int innerLeft = _panel.X + PadX;
        int innerWidth = _panel.Width - PadX * 2;

        int yTop = _panel.Y + topBand;
        int yBottom = _panel.Bottom - bottomBand;
        int availH = Math.Max(0, yBottom - yTop);

        // Fit 3 buttons + 2 gaps into availH.
        // If panel is too short, shrink button height and/or gaps.
        int gap = WantGap;
        int btnH = WantButtonH;

        int needed = btnH * 3 + gap * 2;

        if (needed > availH)
        {
            // First, reduce gaps down to 2
            gap = Math.Max(2, gap - (needed - availH));
            needed = btnH * 3 + gap * 2;
        }

        if (needed > availH)
        {
            // Then shrink button height (but keep it readable)
            int maxBtnH = Math.Max(20, (availH - gap * 2) / 3);
            btnH = Math.Min(btnH, maxBtnH);
            needed = btnH * 3 + gap * 2;
        }

        // Center stack vertically within available region
        int startY = yTop + Math.Max(0, (availH - needed) / 2);

        _btnResume = new Rectangle(innerLeft, startY + 0 * (btnH + gap), innerWidth, btnH);
        _btnInventory = new Rectangle(innerLeft, startY + 1 * (btnH + gap), innerWidth, btnH);
        _btnMenu = new Rectangle(innerLeft, startY + 2 * (btnH + gap), innerWidth, btnH);
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

        if (Pressed(ks, Keys.Back) || Pressed(pad, Buttons.B) || Pressed(pad, Buttons.Back))
        {
            _s.Scenes.Pop();
            _prevKs = ks;
            _prevPad = pad;
            return;
        }

        if (Pressed(ks, Keys.Up) || Pressed(ks, Keys.W) || Pressed(pad, Buttons.DPadUp))
            _focus = Math.Max(0, _focus - 1);

        if (Pressed(ks, Keys.Down) || Pressed(ks, Keys.S) || Pressed(pad, Buttons.DPadDown))
            _focus = Math.Min(2, _focus + 1);

        if (Pressed(ks, Keys.Enter) || Pressed(ks, Keys.Space) || Pressed(pad, Buttons.A))
        {
            if (_focus == 0) _s.Scenes.Pop();
            else if (_focus == 1) _s.Scenes.Push(new MenuHubScene(_s, ks, pad, startTab: 0));
            else _s.Scenes.Replace(new BootScene(_s, ks, pad));

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

        sb.Draw(_white,
            new Rectangle(0, 0, PixelRenderer.InternalWidth, PixelRenderer.InternalHeight),
            Color.Black * 0.65f);

        MenuRenderer.DrawPanel(sb, _white, _panel, new Color(20, 20, 40) * 0.98f);

        // Title centered
        const string title = "PAUSED";
        var titleSize = _s.TitleFont.Measure(title, TitleScale);
        int titleX = _panel.X + (_panel.Width - titleSize.X) / 2;
        int titleY = _panel.Y + TitleTop;
        _s.TitleFont.DrawShadow(sb, title, new Vector2(titleX, titleY), Color.White, scale: TitleScale);

        // Buttons
        MenuRenderer.DrawButton(sb, _white, _s.UiFont, _btnResume, "RESUME",
            focused: _focus == 0, enabled: true, timeSeconds: t, fontScale: 2);

        MenuRenderer.DrawButton(sb, _white, _s.UiFont, _btnInventory, "INVENTORY",
            focused: _focus == 1, enabled: true, timeSeconds: t, fontScale: 2);

        MenuRenderer.DrawButton(sb, _white, _s.UiFont, _btnMenu, "MENU",
            focused: _focus == 2, enabled: true, timeSeconds: t, fontScale: 2);

        // Footer (single centered line so it can never collide)
        string footer = "ENTER/A: SELECT   BACK/B: RETURN";

        int footerH = _s.TitleFont.LineHeight(FooterScale);
        int footerY = _panel.Bottom - FooterPad - footerH;

        var footerSize = _s.TitleFont.Measure(footer, FooterScale);
        int footerX = _panel.X + (_panel.Width - footerSize.X) / 2;

        _s.TitleFont.Draw(sb, footer, new Vector2(footerX, footerY), Color.White * 0.80f, scale: FooterScale);

        sb.End();
    }
}
