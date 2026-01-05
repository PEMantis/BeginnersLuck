using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BeginnersLuck.Engine.Graphics;

public sealed class SpriteAtlas
{
    private readonly Texture2D _tex;
    private readonly Dictionary<string, SpriteRef> _sprites = new(StringComparer.OrdinalIgnoreCase);

    public Texture2D Texture => _tex;

    public SpriteAtlas(Texture2D texture) => _tex = texture;

    public void Add(string id, Rectangle src, Vector2 origin)
        => _sprites[id] = new SpriteRef(_tex, src, origin);

    public bool TryGet(string id, out SpriteRef s) => _sprites.TryGetValue(id, out s);

    public SpriteRef Get(string id)
        => _sprites.TryGetValue(id, out var s)
            ? s
            : throw new KeyNotFoundException($"Sprite not found in atlas: {id}");
}
