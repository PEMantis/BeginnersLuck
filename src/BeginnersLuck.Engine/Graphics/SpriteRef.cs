using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BeginnersLuck.Engine.Graphics;

public readonly record struct SpriteRef(Texture2D Texture, Rectangle Src, Vector2 Origin)
{
    public static SpriteRef Whole(Texture2D tex)
        => new(tex, new Rectangle(0, 0, tex.Width, tex.Height), Vector2.Zero);
}
