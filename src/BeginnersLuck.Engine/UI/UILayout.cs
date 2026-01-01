using Microsoft.Xna.Framework;

namespace BeginnersLuck.Engine.UI;

public static class UiLayout
{
    public static Rectangle Inset(Rectangle r, int pad)
        => new(r.X + pad, r.Y + pad, r.Width - pad * 2, r.Height - pad * 2);

    public static Rectangle Centered(Rectangle bounds, int w, int h)
        => new(bounds.X + (bounds.Width - w) / 2, bounds.Y + (bounds.Height - h) / 2, w, h);

    public static Rectangle MoveY(Rectangle r, int dy)
        => new(r.X, r.Y + dy, r.Width, r.Height);

    public static Rectangle MoveX(Rectangle r, int dx)
        => new(r.X + dx, r.Y, r.Width, r.Height);

    public static Rectangle WithHeight(Rectangle r, int h)
        => new(r.X, r.Y, r.Width, h);

    public static Rectangle WithWidth(Rectangle r, int w)
        => new(r.X, r.Y, w, r.Height);
}
