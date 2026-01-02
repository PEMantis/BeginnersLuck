using Microsoft.Xna.Framework;

namespace BeginnersLuck.Engine.UI;

/// <summary>
/// Standard layout rectangles for "full screen panel" UIs at 640x360.
/// Keeps spacing consistent across every menu page.
/// </summary>
public readonly struct PanelLayout
{
    public PanelLayout(
        Rectangle screen,
        int margin,
        int gutter,
        int headerH,
        int footerH,
        int leftW)
    {
        Screen = screen;
        Margin = margin;
        Gutter = gutter;

        Outer = Inflate(screen, -margin, -margin);

        Header = new Rectangle(Outer.X, Outer.Y, Outer.Width, headerH);
        Footer = new Rectangle(Outer.X, Outer.Bottom - footerH, Outer.Width, footerH);

        Body = new Rectangle(
            Outer.X,
            Header.Bottom + gutter,
            Outer.Width,
            Outer.Height - headerH - footerH - gutter * 2);

        Left = new Rectangle(Body.X, Body.Y, leftW, Body.Height);
        Right = new Rectangle(Left.Right + gutter, Body.Y, Body.Width - leftW - gutter, Body.Height);

        // Common sub-panels inside Right
        RightTop = new Rectangle(Right.X, Right.Y, Right.Width, (int)(Right.Height * 0.62f));
        RightBottom = new Rectangle(Right.X, RightTop.Bottom + gutter, Right.Width, Right.Height - RightTop.Height - gutter);
    }

    public Rectangle Screen { get; }
    public int Margin { get; }
    public int Gutter { get; }

    public Rectangle Outer { get; }
    public Rectangle Header { get; }
    public Rectangle Footer { get; }
    public Rectangle Body { get; }

    // Typical 2-column layout
    public Rectangle Left { get; }
    public Rectangle Right { get; }

    // Optional split inside Right
    public Rectangle RightTop { get; }
    public Rectangle RightBottom { get; }

    public static PanelLayout CreateDefault640x360()
    {
        var screen = new Rectangle(0, 0, 640, 360);

        // Tuned for PressStart2P 14px + scale 1..2
        return new PanelLayout(
            screen: screen,
            margin: 12,
            gutter: 8,
            headerH: 44,
            footerH: 28,
            leftW: 250
        );
    }

    private static Rectangle Inflate(Rectangle r, int dx, int dy)
        => new Rectangle(r.X + dx, r.Y + dy, r.Width - dx * 2, r.Height - dy * 2);
}
