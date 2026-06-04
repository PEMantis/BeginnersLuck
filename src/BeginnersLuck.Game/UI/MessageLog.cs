using System;
using System.Collections.Generic;
using BeginnersLuck.Engine.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BeginnersLuck.Game.UI;

public sealed class MessageLog
{
    private readonly List<string> _entries = new();

    public int Capacity { get; }

    public MessageLog(int capacity = 10)
    {
        Capacity = Math.Max(1, capacity);
    }

    public IReadOnlyList<string> Entries => _entries;

    public void Add(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        _entries.Add(text);

        int overflow = _entries.Count - Capacity;
        if (overflow > 0)
            _entries.RemoveRange(0, overflow);
    }

    public void Draw(SpriteBatch sb, Texture2D white, IFont font, Rectangle area, int scale = 1)
    {
        if (_entries.Count == 0)
            return;

        sb.Draw(white, area, new Color(8, 8, 14) * 0.78f);

        int y = area.Y + 6;
        for (int i = 0; i < _entries.Count; i++)
        {
            font.Draw(sb, _entries[i], new Vector2(area.X + 8, y), Color.White * 0.88f, scale);
            y += font.LineHeight(scale);
            if (y > area.Bottom - 10)
                break;
        }
    }
}
