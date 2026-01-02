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

namespace BeginnersLuck.Game.Scenes;

/// <summary>
/// Base scene for any full-screen menu/page UI.
/// Provides:
/// - shared background dim
/// - standard panel layout (640x360)
/// - header/footer drawing helpers
/// - safe input "press-through" guard
/// </summary>
public abstract class PanelScreenBase : SceneBase
{
    protected readonly GameServices S;
    private Texture2D? _white;

    protected PanelLayout Layout { get; private set; }

    protected bool EatFirstUpdate { get; set; } = true;

    protected PanelScreenBase(GameServices s)
    {
        S = s ?? throw new ArgumentNullException(nameof(s));
        Layout = PanelLayout.CreateDefault640x360();
    }

    public override void Load(GraphicsDevice graphicsDevice, ContentManager content)
    {
        _white = new Texture2D(graphicsDevice, 1, 1);
        _white.SetData(new[] { Color.White });

        OnLoad();
    }

    public override void Unload()
    {
        OnUnload();
        _white?.Dispose();
        _white = null;
    }

    public sealed override void Update(UpdateContext uc)
    {
        if (EatFirstUpdate)
        {
            EatFirstUpdate = false;
            uc.Actions.ConsumeAll();
            return;
        }

        UpdateScreen(uc);
    }

    protected sealed override void DrawUI(RenderContext rc)
    {
        if (_white == null) return;

        var sb = rc.SpriteBatch;

        sb.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.AlphaBlend);

        // Dim the game behind
        sb.Draw(_white, new Rectangle(0, 0, PixelRenderer.InternalWidth, PixelRenderer.InternalHeight),
            Color.Black * 0.65f);

        // Outer frame panel
        MenuRenderer.DrawPanel(sb, _white, Layout.Outer, new Color(16, 16, 28) * 0.98f);

        // Header / footer backing
        MenuRenderer.DrawPanel(sb, _white, Layout.Header, new Color(12, 12, 22) * 0.98f);
        MenuRenderer.DrawPanel(sb, _white, Layout.Footer, new Color(12, 12, 22) * 0.98f);

        // Body columns
        MenuRenderer.DrawPanel(sb, _white, Layout.Left, new Color(18, 18, 34) * 0.98f);
        MenuRenderer.DrawPanel(sb, _white, Layout.Right, new Color(18, 18, 34) * 0.98f);

        DrawHeader(sb);
        DrawFooter(sb);

        DrawBody(sb);

        sb.End();
    }

    // ---- override points ----
    protected virtual void OnLoad() { }
    protected virtual void OnUnload() { }

    protected abstract void UpdateScreen(UpdateContext uc);
    protected abstract void DrawBody(SpriteBatch sb);

    // ---- common header/footer ----
    protected virtual string Title => "MENU";
    protected virtual string FooterHint => "ENTER/A: SELECT   BACK/B: RETURN";

    protected virtual void DrawHeader(SpriteBatch sb)
    {
        // Left aligned title, scale 2
        S.TitleFont.Draw(
            sb,
            Title.ToUpperInvariant(),
            new Vector2(Layout.Header.X + 12, Layout.Header.Y + 10),
            Color.White * 0.95f,
            scale: 2);
    }

    protected virtual void DrawFooter(SpriteBatch sb)
    {
        S.UiFont.Draw(
            sb,
            FooterHint,
            new Vector2(Layout.Footer.X + 12, Layout.Footer.Y + 8),
            Color.White * 0.80f,
            scale: 1);
    }
}
