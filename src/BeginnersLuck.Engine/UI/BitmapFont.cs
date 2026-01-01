using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BeginnersLuck.Engine.UI;

public sealed class BitmapFont
{
    private readonly Texture2D _atlas;
    private readonly int _glyphW, _glyphH, _cols, _firstChar;

    public BitmapFont(Texture2D atlas, int glyphW, int glyphH, int cols, int firstChar = 32)
    {
        _atlas = atlas;
        _glyphW = glyphW;
        _glyphH = glyphH;
        _cols = cols;
        _firstChar = firstChar;
    }

    public void Draw(SpriteBatch sb, string text, Vector2 pos, Color color, int scale = 1)
    {
        int x = (int)pos.X;
        int y = (int)pos.Y;
        int startX = x;

        foreach (char ch in text)
        {
            if (ch == '\n') { x = startX; y += _glyphH * scale; continue; }

            int code = ch - _firstChar;
            if (code < 0) { x += _glyphW * scale; continue; }

            int sx = (code % _cols) * _glyphW;
            int sy = (code / _cols) * _glyphH;

            var src = new Rectangle(sx, sy, _glyphW, _glyphH);
            var dst = new Rectangle(x, y, _glyphW * scale, _glyphH * scale);

            sb.Draw(_atlas, dst, src, color);
            x += _glyphW * scale;
        }
    }
    private static Point MeasureText8x8(string text, int scale)
    {
        // 8x8 fixed font. Treat unknown chars as width 1.
        // Handle newlines so you can expand later.
        int maxLine = 0;
        int line = 0;
        int lines = 1;

        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n')
            {
                maxLine = Math.Max(maxLine, line);
                line = 0;
                lines++;
            }
            else
            {
                line++;
            }
        }
        maxLine = Math.Max(maxLine, line);

        int w = maxLine * 8 * scale;
        int h = lines * 8 * scale;
        return new Point(w, h);
    }

    public Point MeasureFixed8x8(string text, int scale = 1) => MeasureText8x8(text, scale);

}
