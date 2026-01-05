using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using BeginnersLuck.Engine.Graphics;

namespace BeginnersLuck.Engine.Rendering;

public static class SpriteDraw
{
    public static void Draw(
        SpriteBatch sb,
        SpriteRef sprite,
        Vector2 pos,
        Color color,
        float rotation = 0f,
        Vector2? scale = null,
        SpriteEffects fx = SpriteEffects.None,
        float layer = 0f)
    {
        sb.Draw(
            sprite.Texture,
            position: pos,
            sourceRectangle: sprite.Src,
            color: color,
            rotation: rotation,
            origin: sprite.Origin,
            scale: scale ?? Vector2.One,
            effects: fx,
            layerDepth: layer);
    }
}
