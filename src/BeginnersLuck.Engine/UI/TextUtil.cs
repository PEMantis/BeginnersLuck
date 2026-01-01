using System;
using System.Text;
using Microsoft.Xna.Framework;

namespace BeginnersLuck.Engine.UI;

public static class TextUtil
{
    public static string Ellipsize(IFont font, string text, int maxWidth, int scale = 1)
    {
        if (string.IsNullOrEmpty(text)) return "";
        if (font.Measure(text, scale).X <= maxWidth) return text;

        const string dots = "...";
        int lo = 0;
        int hi = text.Length;

        while (lo < hi)
        {
            int mid = (lo + hi) / 2;
            string candidate = text.Substring(0, mid) + dots;
            if (font.Measure(candidate, scale).X <= maxWidth) lo = mid + 1;
            else hi = mid;
        }

        int cut = Math.Max(0, lo - 1);
        return text.Substring(0, cut) + dots;
    }

    public static string Wrap(IFont font, string text, int maxWidth, int scale = 1)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";

        var sb = new StringBuilder();
        var word = new StringBuilder();
        int lineW = 0;

        void FlushWord()
        {
            if (word.Length == 0) return;

            string w = word.ToString();
            int wW = font.Measure(w, scale).X;

            // If word itself is wider than the line, hard-split it
            if (wW > maxWidth)
            {
                for (int i = 0; i < w.Length; i++)
                {
                    string ch = w[i].ToString();
                    int chW = font.Measure(ch, scale).X;

                    if (lineW + chW > maxWidth && lineW > 0)
                    {
                        sb.Append('\n');
                        lineW = 0;
                    }

                    sb.Append(ch);
                    lineW += chW;
                }
            }
            else
            {
                if (lineW + wW > maxWidth && lineW > 0)
                {
                    sb.Append('\n');
                    lineW = 0;
                }

                sb.Append(w);
                lineW += wW;
            }

            word.Clear();
        }

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            if (c == '\n')
            {
                FlushWord();
                sb.Append('\n');
                lineW = 0;
                continue;
            }

            if (char.IsWhiteSpace(c))
            {
                FlushWord();

                // collapse multiple spaces
                if (lineW == 0) continue;

                int spaceW = font.Measure(" ", scale).X;
                if (lineW + spaceW > maxWidth)
                {
                    sb.Append('\n');
                    lineW = 0;
                }
                else
                {
                    sb.Append(' ');
                    lineW += spaceW;
                }
                continue;
            }

            word.Append(c);
        }

        FlushWord();
        return sb.ToString();
    }

    public static Vector2 CenteredPos(IFont font, string text, Rectangle r, int scale = 1)
    {
        var size = font.Measure(text, scale);
        return new Vector2(
            r.X + (r.Width - size.X) * 0.5f,
            r.Y + (r.Height - size.Y) * 0.5f
        );
    }
}
