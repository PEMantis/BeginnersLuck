using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BeginnersLuck.Engine.UI;

public interface IFont : System.IDisposable
{
    int LineHeight(int scale = 1);

    // Measures using the same layout rules as DrawString (newlines respected).
    Point Measure(string text, int scale = 1);

    // Draws at pixel-aligned coordinates (recommended for crisp UI).
    void DrawString(SpriteBatch sb, string text, Vector2 pos, Color color, int scale = 1);

    // Optional: draw clipped to a rectangle (UI panels).
    void DrawStringClipped(SpriteBatch sb, string text, Rectangle clip, Vector2 pos, Color color, int scale = 1);
}
