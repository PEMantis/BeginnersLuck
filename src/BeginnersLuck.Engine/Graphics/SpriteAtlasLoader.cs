using System;
using System.Collections.Generic;
using System.Text.Json;
using BeginnersLuck.Engine.Content;
using Microsoft.Xna.Framework;

namespace BeginnersLuck.Engine.Graphics;

public static class SpriteAtlasLoader
{
    private sealed class AtlasJson
    {
        public string Image { get; set; } = "";
        public Dictionary<string, SpriteJson> Sprites { get; set; } = new();
    }

    private sealed class SpriteJson
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int W { get; set; }
        public int H { get; set; }

        // origin in pixels (optional)
        public int? Ox { get; set; }
        public int? Oy { get; set; }
    }

    public static SpriteAtlas Load(RawContent raw, string atlasJsonPath)
    {
        var jsonText = raw.LoadText(atlasJsonPath);
        var data = JsonSerializer.Deserialize<AtlasJson>(jsonText, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException($"Failed to parse atlas json: {atlasJsonPath}");

        var tex = raw.LoadTexture(data.Image);
        var atlas = new SpriteAtlas(tex);

        foreach (var kvp in data.Sprites)
        {
            var id = kvp.Key;
            var s = kvp.Value;

            var src = new Rectangle(s.X, s.Y, s.W, s.H);

            // Default origin: bottom-center (great for trees/mountains)
            var origin = new Vector2(
                s.Ox ?? (s.W / 2f),
                s.Oy ?? (s.H - 1));

            atlas.Add(id, src, origin);
        }

        return atlas;
    }
}
