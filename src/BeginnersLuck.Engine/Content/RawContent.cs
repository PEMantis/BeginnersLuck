using Microsoft.Xna.Framework.Graphics;

namespace BeginnersLuck.Engine.Content;

public sealed class RawContent
{
    private readonly GraphicsDevice _gd;
    private readonly string _rootAbs;
    private readonly Dictionary<string, Texture2D> _textures = new(StringComparer.OrdinalIgnoreCase);

    public RawContent(GraphicsDevice gd, string rootFolderName = "ContentRaw")
    {
        _gd = gd;

        // BaseDirectory = .../bin/Debug/net9.0/ (or similar)
        _rootAbs = Path.Combine(AppContext.BaseDirectory, rootFolderName);
    }

    public Texture2D LoadTexture(string relativePath)
    {
        if (_textures.TryGetValue(relativePath, out var cached) && !cached.IsDisposed)
            return cached;

        var full = Path.Combine(_rootAbs, relativePath);
        using var fs = File.OpenRead(full);
        var tex = Texture2D.FromStream(_gd, fs);
        _textures[relativePath] = tex;
        return tex;
    }

    public string LoadText(string relativePath)
        => File.ReadAllText(Path.Combine(_rootAbs, relativePath));
}
