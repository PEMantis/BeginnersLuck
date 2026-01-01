using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BeginnersLuck.Engine.World;

public sealed class TileSet
{
    public Texture2D Texture { get; }
    public int TileSize { get; }
    public int Columns { get; }

    public TileSet(Texture2D texture, int tileSize)
    {
        Texture = texture;
        TileSize = tileSize;
        Columns = texture.Width / tileSize;
    }

    public Rectangle SourceRect(int tileId)
    {
        if (tileId < 0) return Rectangle.Empty;

        int x = (tileId % Columns) * TileSize;
        int y = (tileId / Columns) * TileSize;
        return new Rectangle(x, y, TileSize, TileSize);
    }
}
