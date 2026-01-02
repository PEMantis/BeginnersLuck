using System;
using System.Collections.Generic;
using BeginnersLuck.Engine.Input;
using BeginnersLuck.Engine.UI;
using BeginnersLuck.Engine.Update;
using BeginnersLuck.Game.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BeginnersLuck.Game.Menu;

/// <summary>
/// Reusable "List + Details" page scaffold for the hub.
/// Features:
/// - Left list + right details split
/// - Focus + scroll + repeat nav
/// - Optional per-row "disabled" styling + reason tag
/// - Toast
/// - Simple word-wrap helper for details text
/// </summary>
public abstract class ListDetailsPageBase : IMenuPage
{
    public abstract string Title { get; }
    public virtual string FooterHint => "ENTER/A: SELECT   BACK/B: CLOSE";

    // Pane rectangles (computed during Draw)
    protected Rectangle ListRect { get; private set; }
    protected Rectangle DetailsRect { get; private set; }

    // Config
    protected virtual float SplitLeftFrac => 0.52f; // % of inner width for list
    protected virtual int Gap => 10;
    protected virtual int InnerPad => 10;

    // State
    protected int FocusIndex;
    protected int Scroll;

    private bool _eatFirstUpdate = true;

    // Toast
    private string _toast = "";
    private float _toastT = 0f;

    public virtual void OnEnter(GameServices s)
    {
        _eatFirstUpdate = true;
        FocusIndex = Math.Clamp(FocusIndex, 0, Math.Max(0, ItemCount(s) - 1));
        Scroll = 0;
        ClampScroll(s);
        _toast = "";
        _toastT = 0f;

        OnEnterPage(s);
    }

    public virtual void OnExit(GameServices s)
    {
        _toast = "";
        _toastT = 0f;
        OnExitPage(s);
    }

    public void Update(GameServices s, in UpdateContext uc)
    {
        if (_eatFirstUpdate)
        {
            _eatFirstUpdate = false;
            uc.Actions.ConsumeAll();
            return;
        }

        float dt = (float)uc.GameTime.ElapsedGameTime.TotalSeconds;
        if (_toastT > 0f) _toastT = MathF.Max(0f, _toastT - dt);

        OnUpdatePage(s, uc);

        int count = ItemCount(s);
        if (count <= 0) return;

        // Navigate with repeat
        var up = uc.Actions.Get(GameAction.MoveUp);
        if (up.Pressed || up.Repeated) MoveFocus(s, -1);

        var dn = uc.Actions.Get(GameAction.MoveDown);
        if (dn.Pressed || dn.Repeated) MoveFocus(s, +1);

        // Confirm
        if (uc.Actions.Pressed(uc.Input, GameAction.Confirm))
        {
            if (FocusIndex >= 0 && FocusIndex < count)
                OnConfirm(s, uc, FocusIndex);

            uc.Actions.ConsumeAll();
        }
    }

    public void Draw(GameServices s, SpriteBatch sb, Rectangle contentRect, float timeSeconds)
    {
        var white = s.PixelWhite;
        if (white == null) return;

        // Inner padded rect
        var inner = new Rectangle(
            contentRect.X + InnerPad,
            contentRect.Y + InnerPad,
            contentRect.Width - InnerPad * 2,
            contentRect.Height - InnerPad * 2
        );

        int leftW = (int)(inner.Width * SplitLeftFrac);

        ListRect = new Rectangle(inner.X, inner.Y, leftW, inner.Height);
        DetailsRect = new Rectangle(inner.X + leftW + Gap, inner.Y, inner.Width - leftW - Gap, inner.Height);

        // Background panels
        MenuRenderer.DrawPanel(sb, white, ListRect, new Color(12, 12, 20) * 0.98f);
        MenuRenderer.DrawPanel(sb, white, DetailsRect, new Color(12, 12, 20) * 0.98f);

        DrawList(s, sb, white);
        DrawDetails(s, sb);

        if (_toastT > 0f && !string.IsNullOrWhiteSpace(_toast))
            DrawToast(s, sb, white, contentRect);
    }

    // -----------------------------
    // Core behavior
    // -----------------------------

    protected void SetToast(string text, float seconds = 1.1f)
    {
        _toast = text ?? "";
        _toastT = seconds;
    }

    protected void MoveFocus(GameServices s, int delta)
    {
        int count = ItemCount(s);
        if (count <= 0) return;

        FocusIndex = Math.Clamp(FocusIndex + delta, 0, count - 1);
        ClampScroll(s);
    }

    protected void ClampScroll(GameServices s)
    {
        int count = ItemCount(s);
        if (count <= 0) { Scroll = 0; FocusIndex = 0; return; }

        int visible = VisibleRows(s);
        if (visible <= 0) visible = 1;

        if (FocusIndex < Scroll) Scroll = FocusIndex;
        if (FocusIndex >= Scroll + visible) Scroll = FocusIndex - visible + 1;

        int maxScroll = Math.Max(0, count - visible);
        Scroll = Math.Clamp(Scroll, 0, maxScroll);
    }

    protected int VisibleRows(GameServices s)
    {
        // leave padding inside list panel
        int listH = ListRect.Height > 0 ? ListRect.Height : 160;
        int rowH = RowHeight(s);
        return Math.Max(1, (listH - 20) / rowH);
    }

    protected virtual int RowHeight(GameServices s) => s.UiFont.LineHeight(1) + 8;

    // -----------------------------
    // Drawing
    // -----------------------------

    private void DrawList(GameServices s, SpriteBatch sb, Texture2D white)
    {
        int count = ItemCount(s);
        int rowH = RowHeight(s);
        int visible = VisibleRows(s);

        int y = ListRect.Y + 10;

        if (count == 0)
        {
            s.UiFont.Draw(sb, "(EMPTY)", new Vector2(ListRect.X + 10, y), Color.White * 0.6f, 1);
            return;
        }

        for (int i = 0; i < visible; i++)
        {
            int idx = Scroll + i;
            if (idx >= count) break;

            bool focused = idx == FocusIndex;

            GetRow(s, idx, out var leftText, out var rightText);

            bool enabled = IsRowEnabled(s, idx, out var reason);
            if (focused && !enabled && !string.IsNullOrWhiteSpace(reason))
            {
                // Replace rightText with reason on focused disabled rows to prevent overlap
                rightText = reason.ToUpperInvariant();
            }

            var r = new Rectangle(ListRect.X + 8, y - 2, ListRect.Width - 16, rowH);

            var fill = focused ? new Color(70, 70, 120) : new Color(40, 40, 70);
            if (!enabled) fill = new Color(25, 25, 35);

            sb.Draw(white, r, fill * 0.90f);

            var outline = focused ? Color.White * 0.55f : Color.White * 0.20f;
            if (!enabled) outline = Color.White * 0.12f;
            MenuRenderer.DrawOutline(sb, white, r, 2, outline);

            leftText = (leftText ?? "").ToUpperInvariant();
            rightText = (rightText ?? "").ToUpperInvariant();

            var leftColor = enabled ? Color.White * 0.92f : Color.White * 0.45f;
            var rightColor = enabled ? Color.White * 0.75f : Color.White * 0.35f;
            if (focused && !enabled && !string.IsNullOrWhiteSpace(reason))
                rightColor = Color.White * 0.25f;

            int rightW = s.UiFont.Measure(rightText, 1).X;
            int maxLeftW = r.Width - 18 - rightW;
            leftText = s.UiFont.TrimToWidth(leftText, maxLeftW, 1);

            s.UiFont.Draw(sb, leftText, new Vector2(r.X + 10, r.Y + 6), leftColor, 1);
            s.UiFont.Draw(sb, rightText, new Vector2(r.Right - 10 - rightW, r.Y + 6), rightColor, 1);

            y += rowH;
        }

        // Scrollbar
        var track = new Rectangle(ListRect.Right - 8, ListRect.Y + 10, 4, ListRect.Height - 20);
        MenuRenderer.DrawScrollBar(sb, white, track, count, Scroll, visible, alpha: 0.45f);
    }

    private void DrawDetails(GameServices s, SpriteBatch sb)
    {
        int x = DetailsRect.X + 12;
        int y = DetailsRect.Y + 12;

        int count = ItemCount(s);
        if (count == 0)
        {
            s.UiFont.Draw(sb, "NO ITEMS.", new Vector2(x, y), Color.White * 0.7f, 1);
            return;
        }

        FocusIndex = Math.Clamp(FocusIndex, 0, count - 1);

        GetDetails(s, FocusIndex,
            out var title,
            out var preview,
            out var body,
            out var footerLine);

        title = (title ?? "").ToUpperInvariant();
        preview = (preview ?? "").ToUpperInvariant();
        body = (body ?? "").ToUpperInvariant();
        footerLine = (footerLine ?? "").ToUpperInvariant();

        // Title
        s.UiFont.Draw(sb, title, new Vector2(x, y), Color.White * 0.95f, 1);
        y += s.UiFont.LineHeight(1) + 8;

        // Preview
        if (!string.IsNullOrWhiteSpace(preview))
        {
            s.UiFont.Draw(sb, preview, new Vector2(x, y), Color.White * 0.80f, 1);
            y += s.UiFont.LineHeight(1) + 10;
        }

        // Body wrap
        int maxW = DetailsRect.Width - 24;
        foreach (var line in Wrap(s, body, maxW))
        {
            s.UiFont.Draw(sb, line, new Vector2(x, y), Color.White * 0.70f, 1);
            y += s.UiFont.LineHeight(1);
            if (y > DetailsRect.Bottom - 30) break;
        }

        // Footer line bottom-right-ish (or bottom-left, your choice)
        if (!string.IsNullOrWhiteSpace(footerLine))
            s.UiFont.Draw(sb, footerLine, new Vector2(x, DetailsRect.Bottom - 22), Color.White * 0.65f, 1);
    }

    private void DrawToast(GameServices s, SpriteBatch sb, Texture2D white, Rectangle contentRect)
    {
        var r = new Rectangle(contentRect.X + 30, contentRect.Y + 10, contentRect.Width - 60, 20);
        sb.Draw(white, r, new Color(10, 10, 18) * 0.88f);
        MenuRenderer.DrawOutline(sb, white, r, 1, Color.White * 0.25f);

        var size = s.UiFont.Measure(_toast, 1);
        var pos = new Vector2(r.X + (r.Width - size.X) / 2, r.Y + 3);
        s.UiFont.Draw(sb, _toast, pos, Color.White * 0.92f, 1);
    }

    protected static IEnumerable<string> Wrap(GameServices s, string text, int maxWidth)
    {
        text = (text ?? "").Replace("\n", " ").Trim();
        while (text.Length > 0)
        {
            var candidate = s.UiFont.TrimToWidth(text, maxWidth, 1);
            if (candidate.Length == text.Length)
            {
                yield return candidate;
                yield break;
            }

            int cut = candidate.LastIndexOf(' ');
            if (cut > 10)
                candidate = candidate[..cut];

            candidate = candidate.TrimEnd();
            if (candidate.Length == 0) yield break;

            yield return candidate;

            text = text[candidate.Length..].TrimStart();
        }
    }

    // -----------------------------
    // Hooks pages implement
    // -----------------------------

    /// <summary>How many list rows?</summary>
    protected abstract int ItemCount(GameServices s);

    /// <summary>Return row left/right text.</summary>
    protected abstract void GetRow(GameServices s, int index, out string left, out string right);

    /// <summary>Return details: title, preview, body, footerLine.</summary>
    protected abstract void GetDetails(GameServices s, int index, out string title, out string preview, out string body, out string footerLine);

    /// <summary>Can the row be "activated/used"? (also controls disabled styling + optional reason)</summary>
    protected virtual bool IsRowEnabled(GameServices s, int index, out string reason)
    {
        reason = "";
        return true;
    }

    /// <summary>Confirm handler.</summary>
    protected virtual void OnConfirm(GameServices s, in UpdateContext uc, int index) { }

    /// <summary>Extra OnEnter/OnExit/Update hooks for derived pages.</summary>
    protected virtual void OnEnterPage(GameServices s) { }
    protected virtual void OnExitPage(GameServices s) { }
    protected virtual void OnUpdatePage(GameServices s, in UpdateContext uc) { }
}
