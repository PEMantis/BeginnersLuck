using Microsoft.Xna.Framework;

namespace BeginnersLuck.Engine.UI;

public sealed class MenuItem
{
    public MenuItem(
        Rectangle bounds,
        System.Action onActivate,
        System.Action? onLeft = null,
        System.Action? onRight = null)
    {
        Bounds = bounds;
        OnActivate = onActivate;
        OnLeft = onLeft;
        OnRight = onRight;
    }

    public Rectangle Bounds { get; }
    public System.Action OnActivate { get; }

    // Optional “adjust” handlers for sliders/tabs/etc.
    public System.Action? OnLeft { get; }
    public System.Action? OnRight { get; }

    public bool Enabled { get; set; } = true;
}
