using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BeginnersLuck.Engine.World;

public sealed class TileMapRenderer
{
    private readonly TileSet _tileset;

    public TileMapRenderer(TileSet tileset)
    {
        _tileset = tileset;
    }

    public void Draw(SpriteBatch sb, TileMap map, Rectangle viewWorldPixels)
    {
        int ts = map.TileSize;

        int minX = Math.Max(0, viewWorldPixels.Left / ts);
        int minY = Math.Max(0, viewWorldPixels.Top / ts);
        int maxX = Math.Min(map.Width - 1, viewWorldPixels.Right / ts + 1);
        int maxY = Math.Min(map.Height - 1, viewWorldPixels.Bottom / ts + 1);

        for (int y = minY; y <= maxY; y++)
        for (int x = minX; x <= maxX; x++)
        {
            int id = map.GetTileId(x, y);
            if (id < 0) continue;

            var src = _tileset.SourceRect(id);
            var dst = new Rectangle(x * ts, y * ts, ts, ts);

            sb.Draw(_tileset.Texture, dst, src, Color.White);
        }
    }
}
