using System;
using System.IO;
using System.Text.Json;

namespace BeginnersLuck.Game.Graphics;

public static class AssetManifestLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static AssetManifest LoadFromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new AssetManifest();

        return JsonSerializer.Deserialize<AssetManifest>(json, Options) ?? new AssetManifest();
    }

    public static AssetManifest LoadFromFile(string path)
    {
        if (!File.Exists(path))
            return new AssetManifest();

        return LoadFromJson(File.ReadAllText(path));
    }
}
