using BeginnersLuck.Engine.Graphics;
using BeginnersLuck.Engine.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BeginnersLuck.Engine.Transitions;

public sealed class FadeTransition
{
    private float _t;
    private float _duration = 0.35f;
    private bool _active;
    private bool _switchDone;

    private Action? _onMidpoint;

    public bool Active => _active;

    public void Start(float durationSeconds, Action onMidpoint)
    {
        _duration = MathF.Max(0.01f, durationSeconds);
        _t = 0f;
        _active = true;
        _switchDone = false;
        _onMidpoint = onMidpoint;
    }

    public void Update(GameTime gt)
    {
        if (!_active) return;

        _t += (float)gt.ElapsedGameTime.TotalSeconds;

        // midpoint triggers scene switch
        if (!_switchDone && _t >= _duration)
        {
            _switchDone = true;
            _onMidpoint?.Invoke();
        }

        // complete after fade-out + fade-in
        if (_t >= _duration * 2f)
        {
            _active = false;
            _onMidpoint = null;
        }
    }

    public void Draw(RenderContext rc)
    {
        if (!_active) return;

        var sb = rc.SpriteBatch;
        var gd = sb.GraphicsDevice;

        float alpha = (_t <= _duration)
            ? _t / _duration
            : 1f - ((_t - _duration) / _duration);

        alpha = MathHelper.Clamp(alpha, 0f, 1f);

        sb.Begin(
            samplerState: SamplerState.PointClamp,
            blendState: BlendState.AlphaBlend);

        sb.Draw(
            GetWhitePixel(gd),
            destinationRectangle: new Rectangle(0, 0, PixelRenderer.InternalWidth, PixelRenderer.InternalHeight),
            color: Color.Black * alpha);

        sb.End();
    }

    private static Texture2D? _white;
    private static Texture2D GetWhitePixel(GraphicsDevice gd)
    {
        if (_white != null && !_white.IsDisposed) return _white;
        _white = new Texture2D(gd, 1, 1);
        _white.SetData(new[] { Color.White });
        return _white;
    }
}
