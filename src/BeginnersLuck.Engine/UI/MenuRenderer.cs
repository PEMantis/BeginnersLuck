using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BeginnersLuck.Engine.UI;

public static class MenuRenderer
{
    public static void DrawPanel(
        SpriteBatch sb,
        Texture2D white,
        Rectangle r,
        Color fill,
        int border = 2,
        float borderAlpha = 0.25f)
    {
        sb.Draw(white, r, fill);
        DrawOutline(sb, white, r, border, Color.White * borderAlpha);
    }

    public static void DrawButton(
       SpriteBatch sb,
       Texture2D white,
       IFont font,
       Rectangle r,
       string text,
       bool focused,
       bool enabled,
       float timeSeconds,
       int fontScale = 1,
       int contentPadX = 22,
       int contentPadY = 6) // give a little vertical breathing room
    {
        var baseFill = focused
            ? new Color(70, 70, 120)
            : new Color(40, 40, 70);

        if (!enabled)
            baseFill = new Color(25, 25, 35);

        sb.Draw(white, r, baseFill * 0.95f);

        var outline = focused ? Color.White * 0.65f : Color.White * 0.25f;
        if (!enabled)
            outline = Color.White * 0.12f;

        DrawOutline(sb, white, r, 2, outline);

        // Text centered in content area using font metrics (no bias magic)
        if (font != null && !string.IsNullOrWhiteSpace(text))
        {
            var content = Inset(r, contentPadX, contentPadY);

            // Measure width from the font, height from LineHeight (more stable than Measure().Y)
            var size = font.Measure(text, fontScale);
            int textW = size.X;
            int textH = font.LineHeight(fontScale);

            int x = content.X + (content.Width - textW) / 2;
            int y = content.Y + (content.Height - textH) / 2;

            // Clamp so we never wander outside the button box
            if (y < r.Y) y = r.Y;
            if (y + textH > r.Bottom) y = r.Bottom - textH;

            font.Draw(sb, text, new Vector2(x, y), enabled ? Color.White : Color.White * 0.55f, fontScale);
        }

        if (focused && enabled)
            DrawFocusChevrons(sb, white, r, timeSeconds);
    }

    public static void DrawFooterHint(
        SpriteBatch sb,
        IFont font,
        Rectangle panel,
        string leftHint,
        string rightHint,
        Color color,
        int scale = 1,
        int padding = 10)
    {
        if (font == null) return;

        int lh = font.LineHeight(scale);

        // Footer strip inside the panel (one line tall)
        var r = new Rectangle(
            panel.X + padding,
            panel.Bottom - padding - lh,
            panel.Width - padding * 2,
            lh);

        // Optional safety: if panel is too small, do nothing rather than scribbling over UI
        if (r.Width <= 0 || r.Height <= 0) return;

        // If hints are long, trim so they never collide
        int rightW = font.Measure(rightHint, scale).X;
        int gap = 12;

        // Available for left = total - right - gap
        int leftMax = Math.Max(0, r.Width - rightW - gap);

        leftHint = font.TrimToWidth(leftHint, leftMax, scale);

        // Draw left
        font.Draw(sb, leftHint, new Vector2(r.X, r.Y), color, scale);

        // Draw right
        rightW = font.Measure(rightHint, scale).X;
        font.Draw(sb, rightHint, new Vector2(r.Right - rightW, r.Y), color, scale);
    }



    private static Rectangle Inset(Rectangle r, int padX, int padY)
        => new Rectangle(r.X + padX, r.Y + padY, r.Width - padX * 2, r.Height - padY * 2);

    private static void DrawTextCentered(
        SpriteBatch sb,
        IFont font,
        string text,
        Rectangle r,
        Color color,
        int scale,
        int biasY)
    {
        var size = font.Measure(text, scale);

        // Center within r, then apply a tiny bias for visual centering with pixel fonts
        int x = r.X + (r.Width - size.X) / 2;
        int y = r.Y + (r.Height - size.Y) / 2 + biasY;

        font.DrawString(sb, text, new Vector2(x, y), color, scale);
    }

    private static void DrawFocusChevrons(SpriteBatch sb, Texture2D white, Rectangle r, float timeSeconds)
    {
        int bob = (int)(MathF.Sin(timeSeconds * 10f) * 1.5f);
        int y = r.Y + r.Height / 2 - 4 + bob;

        DrawChevron(sb, white, new Point(r.X + 8, y), left: true);
        DrawChevron(sb, white, new Point(r.Right - 14, y), left: false);
    }

    private static void DrawChevron(SpriteBatch sb, Texture2D white, Point p, bool left)
    {
        Span<int> widths = stackalloc int[] { 1, 2, 3, 2, 1 };

        for (int i = 0; i < widths.Length; i++)
        {
            int w = widths[i];
            int y = p.Y + i;
            int x = left ? p.X + (3 - w) : p.X;

            sb.Draw(white, new Rectangle(x, y, w, 1), Color.White * 0.75f);
        }
    }

    public static void DrawOutline(
        SpriteBatch sb,
        Texture2D white,
        Rectangle r,
        int t,
        Color c)
    {
        sb.Draw(white, new Rectangle(r.X, r.Y, r.Width, t), c);
        sb.Draw(white, new Rectangle(r.X, r.Bottom - t, r.Width, t), c);
        sb.Draw(white, new Rectangle(r.X, r.Y, t, r.Height), c);
        sb.Draw(white, new Rectangle(r.Right - t, r.Y, t, r.Height), c);
    }

    public static void DrawScrollBar(
    SpriteBatch sb,
    Texture2D white,
    Rectangle track,
    int totalItems,
    int firstVisibleIndex,
    int visibleCount,
    float alpha = 0.35f)
    {
        if (totalItems <= 0 || visibleCount <= 0) return;

        // Track
        sb.Draw(white, track, Color.White * (alpha * 0.20f));
        DrawOutline(sb, white, track, 1, Color.White * (alpha * 0.35f));

        // If everything fits, thumb fills track
        if (totalItems <= visibleCount)
        {
            sb.Draw(white, new Rectangle(track.X + 1, track.Y + 1, track.Width - 2, track.Height - 2), Color.White * (alpha * 0.25f));
            return;
        }

        // Thumb size proportional to visible fraction
        float frac = MathHelper.Clamp(visibleCount / (float)totalItems, 0.08f, 1f);
        int thumbH = Math.Max(8, (int)((track.Height - 2) * frac));

        // Thumb position proportional to scroll
        float t = firstVisibleIndex / (float)(totalItems - visibleCount);
        int thumbY = track.Y + 1 + (int)((track.Height - 2 - thumbH) * t);

        var thumb = new Rectangle(track.X + 1, thumbY, track.Width - 2, thumbH);
        sb.Draw(white, thumb, Color.White * (alpha * 0.55f));
    }

    public static Rectangle PushScissor(SpriteBatch sb, Rectangle clip)
    {
        var gd = sb.GraphicsDevice;
        var prev = gd.ScissorRectangle;

        // Scissor must be within the current viewport/backbuffer
        var vp = gd.Viewport.Bounds;
        clip = Rectangle.Intersect(vp, clip);

        gd.ScissorRectangle = Rectangle.Intersect(prev, clip);
        return prev;
    }

    public static void PopScissor(SpriteBatch sb, Rectangle previous)
    {
        sb.GraphicsDevice.ScissorRectangle = previous;
    }

    public static void BeginScissor(SpriteBatch sb, GraphicsDevice gd, Rectangle scissorRect)
    {
        // End the current Begin, then restart with scissor enabled.
        sb.End();

        var prev = gd.ScissorRectangle;
        gd.ScissorRectangle = scissorRect;

        sb.Begin(
            samplerState: SamplerState.PointClamp,
            blendState: BlendState.AlphaBlend,
            rasterizerState: new RasterizerState { ScissorTestEnable = true });

        // NOTE: caller must restore scissor + begin state after drawing clipped content
    }

    public static void EndScissor(SpriteBatch sb, GraphicsDevice gd, Rectangle prevScissor)
    {
        sb.End();
        gd.ScissorRectangle = prevScissor;

        sb.Begin(
            samplerState: SamplerState.PointClamp,
            blendState: BlendState.AlphaBlend);
    }
}
