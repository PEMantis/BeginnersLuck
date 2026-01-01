using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BeginnersLuck.Engine.UI;

public static class FontExtensions
{
    // Legacy API: font.Draw(...)
    public static void Draw(this IFont font, SpriteBatch sb, string text, Vector2 pos, Color color, int scale = 1)
        => font.DrawString(sb, text, pos, color, scale);

    // Legacy API: font.DrawShadow(...)
    public static void DrawShadow(this IFont font, SpriteBatch sb, string text, Vector2 pos, Color color, int scale = 1)
    {
        // classic 1px shadow, scaled
        var shadowOffset = new Vector2(1 * scale, 1 * scale);
        font.DrawString(sb, text, pos + shadowOffset, Color.Black * 0.75f, scale);
        font.DrawString(sb, text, pos, color, scale);
    }

    // Legacy API: font.LineH(...)
    public static int LineH(this IFont font, int scale = 1)
        => font.LineHeight(scale);

    // Legacy API: font.TrimToWidth(...)
    public static string TrimToWidth(this IFont font, string text, int maxWidth, int scale = 1)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        if (font.Measure(text, scale).X <= maxWidth) return text;

        // keep room for ellipsis
        const string ellipsis = "…";
        int target = Math.Max(0, maxWidth - font.Measure(ellipsis, scale).X);

        // quick trim loop (good enough for UI)
        int len = text.Length;
        while (len > 0)
        {
            string candidate = text.Substring(0, len);
            if (font.Measure(candidate, scale).X <= target)
                return candidate + ellipsis;
            len--;
        }

        return ellipsis;
    }
}
