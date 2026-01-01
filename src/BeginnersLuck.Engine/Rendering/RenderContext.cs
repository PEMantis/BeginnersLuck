using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using BeginnersLuck.Engine.Graphics;

namespace BeginnersLuck.Engine.Rendering;

public readonly struct RenderContext
{
    public RenderContext(GameTime gameTime, PixelRenderer pixel, SpriteBatch spriteBatch)
    {
        GameTime = gameTime;
        Pixel = pixel;
        SpriteBatch = spriteBatch;
    }

    public GameTime GameTime { get; }
    public PixelRenderer Pixel { get; }
    public SpriteBatch SpriteBatch { get; }
}
