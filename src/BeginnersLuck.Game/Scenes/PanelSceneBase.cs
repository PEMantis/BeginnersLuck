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

public abstract class PanelSceneBase : SceneBase
{
    protected readonly GameServices S;
    protected Texture2D? White;

    protected bool EatFirstUpdate = true;

    protected Rectangle PanelRect;
    protected int PanelPadding = 14;

    protected string TitleText = "";
    protected int TitleScale = 2;

    protected bool ShowFooterHint = true;
    protected string FooterHint = "ENTER/A: SELECT   BACK/B: RETURN";

    protected string ToastText = "";
    protected float ToastT = 0f;

    protected PanelSceneBase(GameServices s, Rectangle panelRect, string title)
    {
        S = s ?? throw new ArgumentNullException(nameof(s));
        PanelRect = panelRect;
        TitleText = title ?? "";
    }

    public override void Load(GraphicsDevice graphicsDevice, ContentManager content)
    {
        White = new Texture2D(graphicsDevice, 1, 1);
        White.SetData(new[] { Color.White });

        EatFirstUpdate = true;
        OnLoad(graphicsDevice, content);
    }

    public override void Unload()
    {
        OnUnload();
        White?.Dispose();
        White = null;
    }

    public override void Update(UpdateContext uc)
    {
        if (EatFirstUpdate)
        {
            EatFirstUpdate = false;
            uc.Actions.ConsumeAll();
            return;
        }

        float dt = (float)uc.GameTime.ElapsedGameTime.TotalSeconds;
        if (ToastT > 0f) ToastT = MathF.Max(0f, ToastT - dt);

        if (uc.Actions.Pressed(uc.Input, GameAction.Cancel))
        {
            uc.Actions.ConsumeAll();
            S.Scenes.Pop();
            return;
        }

        OnUpdate(uc);
    }

    protected sealed override void DrawUI(RenderContext rc)
    {
        if (White == null) return;

        var sb = rc.SpriteBatch;

        sb.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.AlphaBlend);

        // ✅ Dim the entire INTERNAL surface (virtual resolution)
        sb.Draw(
            White,
            new Rectangle(0, 0, PixelRenderer.InternalWidth, PixelRenderer.InternalHeight),
            Color.Black * 0.70f);

        // Main panel
        MenuRenderer.DrawPanel(sb, White, PanelRect, new Color(18, 18, 34) * 0.98f);

        // Title
        if (!string.IsNullOrWhiteSpace(TitleText))
        {
            var titleSize = S.TitleFont.Measure(TitleText, TitleScale);
            int tx = PanelRect.X + (PanelRect.Width - titleSize.X) / 2;
            int ty = PanelRect.Y + 10;
            S.TitleFont.DrawShadow(sb, TitleText.ToUpperInvariant(), new Vector2(tx, ty), Color.White, TitleScale);
        }

        var content = ContentRect();
        DrawPanelContent(sb, content, rc);

        if (ShowFooterHint && !string.IsNullOrWhiteSpace(FooterHint))
        {
            var footerSize = S.UiFont.Measure(FooterHint, 1);
            int fx = PanelRect.X + (PanelRect.Width - footerSize.X) / 2;
            int fy = PanelRect.Bottom - 18;
            S.UiFont.Draw(sb, FooterHint.ToUpperInvariant(), new Vector2(fx, fy), Color.White * 0.75f, 1);
        }

        if (ToastT > 0f && !string.IsNullOrWhiteSpace(ToastText))
            DrawToast(sb, PanelRect);

        sb.End();
    }

    protected Rectangle ContentRect()
    {
        int titleBand = 10 + S.TitleFont.LineHeight(TitleScale) + 10;
        int bottomBand = ShowFooterHint ? 24 : 10;

        return new Rectangle(
            PanelRect.X + PanelPadding,
            PanelRect.Y + titleBand,
            PanelRect.Width - PanelPadding * 2,
            PanelRect.Height - titleBand - bottomBand - PanelPadding / 2
        );
    }

    protected void SetToast(string text, float seconds = 1.2f)
    {
        ToastText = text ?? "";
        ToastT = seconds;
    }

    protected void DrawToast(SpriteBatch sb, Rectangle panelRect)
    {
        if (ToastT <= 0f || White == null) return;

        var r = new Rectangle(panelRect.X + 80, panelRect.Y + 44, panelRect.Width - 160, 20);
        sb.Draw(White, r, new Color(10, 10, 18) * 0.88f);
        MenuRenderer.DrawOutline(sb, White, r, 1, Color.White * 0.25f);

        var size = S.UiFont.Measure(ToastText, 1);
        var pos = new Vector2(r.X + (r.Width - size.X) / 2, r.Y + 3);
        S.UiFont.Draw(sb, ToastText, pos, Color.White * 0.92f, 1);
    }

    protected virtual void OnLoad(GraphicsDevice graphicsDevice, ContentManager content) { }
    protected virtual void OnUnload() { }
    protected abstract void OnUpdate(UpdateContext uc);
    protected abstract void DrawPanelContent(SpriteBatch sb, Rectangle content, RenderContext rc);
}
