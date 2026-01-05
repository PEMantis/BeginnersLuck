using System;
using System.Collections.Generic;
using BeginnersLuck.Engine.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BeginnersLuck.Game.Graphics;

public sealed class SpriteDb
{
    private readonly RawContent _raw;
    private readonly Dictionary<string, Texture2D> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Vector2> _origin = new(StringComparer.OrdinalIgnoreCase);

    public SpriteDb(RawContent raw) => _raw = raw;

    /// <summary>
    /// Registers a sprite id -> png path. Origin defaults to bottom-center.
    /// </summary>
    public void Register(string id, string relativePngPath, Vector2? originPx = null)
    {
        var tex = _raw.LoadTexture(relativePngPath);
        _cache[id] = tex;

        var o = originPx ?? new Vector2(tex.Width / 2f, tex.Height - 1);
        _origin[id] = o;
    }

    public bool TryGet(string id, out Texture2D tex, out Vector2 origin)
    {
        if (_cache.TryGetValue(id, out tex!))
        {
            origin = _origin[id];
            return true;
        }

        tex = null!;
        origin = default;
        return false;
    }

    public (Texture2D Tex, Vector2 Origin) Get(string id)
    {
        if (!TryGet(id, out var tex, out var origin))
            throw new KeyNotFoundException($"Sprite not registered: {id}");
        return (tex, origin);
    }
}
