using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BeginnersLuck.Engine.Graphics;

public sealed class PixelRenderer : IDisposable
{
    public const int InternalWidth = 480;
    public const int InternalHeight = 270;

    private readonly GraphicsDevice _gd;
    private RenderTarget2D _rt;

    public SpriteBatch SpriteBatch { get; }

    // Presentation info (single source of truth)
    public int BackBufferWidth { get; private set; }
    public int BackBufferHeight { get; private set; }
    public int Scale { get; private set; } = 1;
    public Rectangle DestinationRect { get; private set; } // where the virtual RT is drawn

    public PixelRenderer(GraphicsDevice graphicsDevice)
    {
        _gd = graphicsDevice;
        SpriteBatch = new SpriteBatch(_gd);
        _rt = CreateRenderTarget();
        OnBackBufferChanged(_gd.PresentationParameters.BackBufferWidth,
                            _gd.PresentationParameters.BackBufferHeight);
    }

    public void Dispose()
    {
        _rt.Dispose();
        SpriteBatch.Dispose();
    }

    public void OnBackBufferChanged(int backBufferWidth, int backBufferHeight)
    {
        BackBufferWidth = backBufferWidth;
        BackBufferHeight = backBufferHeight;

        var sx = backBufferWidth / InternalWidth;
        var sy = backBufferHeight / InternalHeight;
        Scale = Math.Max(1, Math.Min(sx, sy));

        var scaledW = InternalWidth * Scale;
        var scaledH = InternalHeight * Scale;

        var offsetX = (backBufferWidth - scaledW) / 2;
        var offsetY = (backBufferHeight - scaledH) / 2;

        DestinationRect = new Rectangle(offsetX, offsetY, scaledW, scaledH);
    }

    // Screen -> virtual pixel (use for mouse/UI). Returns false if outside the game area (letterbox).
    public bool TryScreenToVirtual(Point screen, out Point virtualPoint)
    {
        if (!DestinationRect.Contains(screen))
        {
            virtualPoint = default;
            return false;
        }

        var x = (screen.X - DestinationRect.X) / Scale;
        var y = (screen.Y - DestinationRect.Y) / Scale;

        // clamp defensively (edge cases at right/bottom)
        x = Math.Clamp(x, 0, InternalWidth - 1);
        y = Math.Clamp(y, 0, InternalHeight - 1);

        virtualPoint = new Point(x, y);
        return true;
    }

    public void BeginWorld(Color clearColor)
    {
        _gd.SetRenderTarget(_rt);
        _gd.Viewport = new Viewport(0, 0, InternalWidth, InternalHeight);

        _gd.Clear(clearColor);
        // You can choose to Begin() here or in the scene; up to your style.
    }

    public void EndWorldToScreen(Color letterboxClearColor)
    {
        _gd.SetRenderTarget(null);
        _gd.Viewport = new Viewport(0, 0, BackBufferWidth, BackBufferHeight);

        _gd.Clear(letterboxClearColor);

        SpriteBatch.Begin(
            samplerState: SamplerState.PointClamp,
            blendState: BlendState.AlphaBlend,
            sortMode: SpriteSortMode.Deferred);

        // Draw RT into destination rectangle (integer scaled + centered)
        SpriteBatch.Draw(_rt, DestinationRect, Color.White);
        SpriteBatch.End();
    }

    private RenderTarget2D CreateRenderTarget()
        => new RenderTarget2D(
            _gd,
            InternalWidth,
            InternalHeight,
            false,
            SurfaceFormat.Color,
            DepthFormat.None,
            0,
            RenderTargetUsage.DiscardContents);
}
