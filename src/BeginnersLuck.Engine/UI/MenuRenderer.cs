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
        int contentPadY = 0,
        int textBiasY = -1)
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

        if (font != null)
        {
            // Content area excludes chevrons + borders so label looks centered
            var content = Inset(r, contentPadX, contentPadY);
            DrawTextCentered(sb, font, text, content, enabled ? Color.White : Color.White * 0.55f, fontScale, textBiasY);
        }

        if (focused && enabled)
            DrawFocusChevrons(sb, white, r, timeSeconds);
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

    private static void DrawOutline(SpriteBatch sb, Texture2D white, Rectangle r, int t, Color c)
    {
        sb.Draw(white, new Rectangle(r.X, r.Y, r.Width, t), c);
        sb.Draw(white, new Rectangle(r.X, r.Bottom - t, r.Width, t), c);
        sb.Draw(white, new Rectangle(r.X, r.Y, t, r.Height), c);
        sb.Draw(white, new Rectangle(r.Right - t, r.Y, t, r.Height), c);
    }
}
