using System.Collections.Generic;

namespace BeginnersLuck.Game.Graphics;

public sealed record AssetManifest
{
    public List<SpriteSource> Sprites { get; init; } = new();

    public bool TryFind(string key, out SpriteSource source)
    {
        for (int i = 0; i < Sprites.Count; i++)
        {
            var sprite = Sprites[i];
            if (string.Equals(sprite.Key, key, System.StringComparison.OrdinalIgnoreCase))
            {
                source = sprite;
                return true;
            }
        }

        source = null!;
        return false;
    }
}
