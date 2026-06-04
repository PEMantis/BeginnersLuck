using BeginnersLuck.Game.Graphics;

public sealed class AssetManifestTests
{
    [Fact]
    public void LoadFromJson_parses_sprite_entries()
    {
        var manifest = AssetManifestLoader.LoadFromJson("""
        {
          "sprites": [
            {
              "key": "item.test",
              "path": "Sprites/World/rock_32x32.png",
              "notes": "test entry"
            }
          ]
        }
        """);

        Assert.True(manifest.TryFind("item.test", out var sprite));
        Assert.Equal("item.test", sprite.Key);
        Assert.Equal("Sprites/World/rock_32x32.png", sprite.Path);
    }

    [Fact]
    public void TryFind_is_case_insensitive_and_returns_false_for_missing_keys()
    {
        var manifest = new AssetManifest
        {
            Sprites =
            [
                new SpriteSource { Key = "actor.player.idle.down", Path = "Sprites/World/player_16x16.png" }
            ]
        };

        Assert.True(manifest.TryFind("ACTOR.PLAYER.IDLE.DOWN", out var sprite));
        Assert.Equal("actor.player.idle.down", sprite.Key);
        Assert.False(manifest.TryFind("missing.key", out _));
    }
}
