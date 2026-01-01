using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BeginnersLuck.Engine.UI;

public sealed class BitmapFont : IFont
{
    private readonly Texture2D _atlas;

    public int GlyphW { get; }
    public int GlyphH { get; }
    public int Cols { get; }
    public int FirstChar { get; }

    // Tweak these to taste (and now they ACTUALLY work consistently)
    public int SpacingX { get; set; } = 1;
    public int SpacingY { get; set; } = 2;

    public int AdvanceX => GlyphW + SpacingX;
    public int LineH => GlyphH + SpacingY;

    public BitmapFont(Texture2D atlas, int glyphW, int glyphH, int cols, int firstChar = 32)
    {
        _atlas = atlas ?? throw new ArgumentNullException(nameof(atlas));
        GlyphW = glyphW;
        GlyphH = glyphH;
        Cols = cols;
        FirstChar = firstChar;
    }

    public void Draw(SpriteBatch sb, string text, Vector2 pos, Color color, int scale = 1)
    {
        if (string.IsNullOrEmpty(text)) return;

        // Snap to integer pixels for crisp point sampling
        int x = (int)pos.X;
        int y = (int)pos.Y;
        int startX = x;

        for (int i = 0; i < text.Length; i++)
        {
            char ch = text[i];

            if (ch == '\n')
            {
                x = startX;
                y += LineH * scale;
                continue;
            }

            int code = ch - FirstChar;
            if (code < 0)
            {
                x += AdvanceX * scale;
                continue;
            }

            int sx = (code % Cols) * GlyphW;
            int sy = (code / Cols) * GlyphH;

            // Guard against out-of-range glyphs (atlas too small)
            if (sx < 0 || sy < 0 || sx + GlyphW > _atlas.Width || sy + GlyphH > _atlas.Height)
            {
                x += AdvanceX * scale;
                continue;
            }

            var src = new Rectangle(sx, sy, GlyphW, GlyphH);
            var dst = new Rectangle(x, y, GlyphW * scale, GlyphH * scale);

            sb.Draw(_atlas, dst, src, color);

            x += AdvanceX * scale; // ✅ spacing-aware advance
        }
    }

    public Point Measure(string text, int scale = 1)
    {
        if (string.IsNullOrEmpty(text)) return Point.Zero;

        int maxChars = 0;
        int lineChars = 0;
        int lines = 1;

        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n')
            {
                maxChars = Math.Max(maxChars, lineChars);
                lineChars = 0;
                lines++;
            }
            else
            {
                lineChars++;
            }
        }
        maxChars = Math.Max(maxChars, lineChars);

        int w = maxChars * AdvanceX * scale;
        int h = lines * LineH * scale;
        return new Point(w, h);
    }

    public string TrimToWidth(string text, int maxPixels, int scale = 1)
    {
        if (string.IsNullOrEmpty(text)) return text;

        if (Measure(text, scale).X <= maxPixels) return text;

        // Use "..." because it’s safe in ASCII atlases
        const string ell = "...";
        int ellW = Measure(ell, scale).X;

        if (ellW >= maxPixels) return ""; // nothing sensible fits

        int maxChars = Math.Max(0, (maxPixels - ellW) / (AdvanceX * scale));
        if (maxChars <= 0) return ell;

        if (text.Length <= maxChars) return text;
        return text.Substring(0, maxChars) + ell;
    }

    public void DrawShadow(SpriteBatch sb, string text, Vector2 pos, Color color, int scale = 1)
    {
        // 1px shadow for readability
        Draw(sb, text, pos + new Vector2(1, 1), Color.Black * 0.6f, scale);
        Draw(sb, text, pos, color, scale);
    }

    public void DrawString(SpriteBatch sb, string text, Vector2 pos, Color color, int scale = 1)
    => Draw(sb, text, pos, color, scale);

    public void DrawStringClipped(SpriteBatch sb, string text, Rectangle clip, Vector2 pos, Color color, int scale = 1)
    {
        // If BitmapFont already supports clipping, call it.
        // Otherwise just draw normally for now (safe default).
        Draw(sb, text, pos, color, scale);
    }

    // inside BitmapFont
    public int LineHeight(int scale = 1)
    {
        return LineH * scale;
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
