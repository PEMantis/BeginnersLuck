using System.Collections.Generic;
using BeginnersLuck.Engine.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BeginnersLuck.Game.UI;

public sealed class ToastQueue
{
    private sealed class Toast
    {
        public string Text = "";
        public float Time;
        public float Duration;
    }

    private readonly Queue<Toast> _queue = new();
    private Toast? _current;

    public void Push(string text, float seconds = 1.4f)
    {
        _queue.Enqueue(new Toast
        {
            Text = text ?? "",
            Duration = MathHelper.Max(0.25f, seconds),
            Time = 0f
        });
    }

    public void Update(float dt)
    {
        if (_current == null)
        {
            if (_queue.Count == 0) return;
            _current = _queue.Dequeue();
        }

        _current.Time += dt;
        if (_current.Time >= _current.Duration)
            _current = null;
    }

    public void Draw(SpriteBatch sb, Texture2D white, IFont font, int screenW, int screenH)
    {
        if (_current == null) return;

        float t = _current.Time;
        float d = _current.Duration;

        // simple ease in/out alpha
        float aIn = MathHelper.Clamp(t / 0.15f, 0f, 1f);
        float aOut = MathHelper.Clamp((d - t) / 0.25f, 0f, 1f);
        float alpha = MathHelper.Min(aIn, aOut);

        var text = _current.Text.ToUpperInvariant();

        int scale = 2;
        int pad = 8;

        int textW = text.Length * 8 * scale;
        int textH = 8 * scale;

        var r = new Rectangle(
            x: (screenW - (textW + pad * 2)) / 2,
            y: screenH - 42,
            width: textW + pad * 2,
            height: textH + pad * 2
        );

        sb.Draw(white, r, Color.Black * (0.70f * alpha));
        sb.Draw(white, new Rectangle(r.X, r.Y, r.Width, 1), Color.White * (0.15f * alpha));

        font.Draw(sb, text, new Vector2(r.X + pad, r.Y + pad), Color.White * alpha, scale);
    }
}
