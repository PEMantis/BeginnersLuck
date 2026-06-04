using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BeginnersLuck.Engine.Content;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BeginnersLuck.Game.Graphics;

public sealed class SpriteDb
{
    private readonly RawContent _primary;
    private readonly RawContent? _fallback;
    private readonly Dictionary<string, SpriteSource> _sources = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Texture2D> _textureCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _missingLogged = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _missingOrder = new();
    private int _manifestSpriteCount;

    public SpriteDb(RawContent raw)
        : this(raw, null)
    {
    }

    public SpriteDb(RawContent primary, RawContent? fallback)
    {
        _primary = primary ?? throw new ArgumentNullException(nameof(primary));
        _fallback = fallback;
    }

    /// <summary>
    /// Registers a sprite id -> PNG path in ContentRaw. Origin defaults to bottom-center.
    /// </summary>
    public void Register(string id, string relativePngPath, Vector2? originPx = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Sprite id is required.", nameof(id));
        if (string.IsNullOrWhiteSpace(relativePngPath))
            throw new ArgumentException("Sprite path is required.", nameof(relativePngPath));

        AddSource(new SpriteSource
        {
            Key = id,
            Path = relativePngPath,
            OriginX = originPx?.X,
            OriginY = originPx?.Y,
            Notes = "Registered directly"
        });
    }

    public void LoadManifest(string relativeJsonPath)
    {
        if (string.IsNullOrWhiteSpace(relativeJsonPath))
            return;

        try
        {
            var manifest = AssetManifestLoader.LoadFromJson(_primary.LoadText(relativeJsonPath));
            _manifestSpriteCount = manifest.Sprites.Count;
            for (int i = 0; i < manifest.Sprites.Count; i++)
                AddSource(manifest.Sprites[i]);
        }
        catch (Exception ex)
        {
            LogMissing($"manifest:{relativeJsonPath}", $"Failed to load asset manifest ({ex.Message})");
        }
    }

    public int ManifestSpriteCount => _manifestSpriteCount;
    public int RegisteredSpriteCount => _sources.Count;
    public int CachedTextureCount => _textureCache.Count;
    public int MissingLogCount => _missingLogged.Count;

    public IReadOnlyList<string> GetRecentMissingMessages(int maxCount = 10)
    {
        if (maxCount <= 0 || _missingOrder.Count == 0)
            return Array.Empty<string>();

        int start = Math.Max(0, _missingOrder.Count - maxCount);
        return _missingOrder.GetRange(start, _missingOrder.Count - start);
    }

    public IReadOnlyList<string> GetRegisteredKeys()
        => _sources.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToArray();

    public void LogStartupDiagnostics(IEnumerable<string> expectedItemKeys, IEnumerable<string> expectedEntityKeys)
    {
        Debug.WriteLine($"[Assets] Manifest sprite entries: {_manifestSpriteCount}; registered keys: {_sources.Count}; cached textures: {_textureCache.Count}; missing logs: {_missingLogged.Count}");
        Console.WriteLine($"[Assets] Manifest sprite entries: {_manifestSpriteCount}; registered keys: {_sources.Count}; cached textures: {_textureCache.Count}; missing logs: {_missingLogged.Count}");

        ReportMissingExpectedKeys("item", expectedItemKeys);
        ReportMissingExpectedKeys("entity", expectedEntityKeys);
        ReportMissingManifestFiles();
    }

    public void DumpStatus()
    {
        Debug.WriteLine($"[Assets] Registered keys: {_sources.Count}; textures: {_textureCache.Count}; missing: {_missingLogged.Count}");
        Console.WriteLine($"[Assets] Registered keys: {_sources.Count}; textures: {_textureCache.Count}; missing: {_missingLogged.Count}");

        foreach (var key in GetRegisteredKeys())
        {
            Debug.WriteLine($"[Assets] key -> {key}");
            Console.WriteLine($"[Assets] key -> {key}");
        }

        foreach (var message in GetRecentMissingMessages(8))
        {
            Debug.WriteLine($"[Assets] recent missing -> {message}");
            Console.WriteLine($"[Assets] recent missing -> {message}");
        }
    }

    public bool TryResolve(string key, out SpriteRenderInfo info, float timeSeconds = 0f)
    {
        info = default;

        if (string.IsNullOrWhiteSpace(key))
            return false;

        if (!_sources.TryGetValue(key, out var source))
        {
            LogMissing(key, "sprite key not registered");
            return false;
        }

        if (!TryLoadTexture(source.Path, out var tex))
            return false;

        if (!TryResolveSourceRect(source, tex, timeSeconds, out var sourceRect))
        {
            LogMissing(key, "sprite source rectangle invalid for texture bounds");
            return false;
        }

        var origin = ResolveOrigin(source, sourceRect);
        var scale = ResolveScale(source);

        info = new SpriteRenderInfo(tex, sourceRect, origin, scale, Missing: false, key);
        return true;
    }

    public bool TryGet(string id, out Texture2D tex, out Vector2 origin)
    {
        if (TryResolve(id, out var info))
        {
            tex = info.Texture;
            origin = info.Origin;
            return true;
        }

        tex = null!;
        origin = default;
        return false;
    }

    public bool TryDraw(SpriteBatch sb, string key, Rectangle destination, Color tint, float timeSeconds = 0f)
    {
        if (!TryResolve(key, out var info, timeSeconds))
            return false;

        sb.Draw(info.Texture, destination, info.Source, tint, 0f, info.Origin, SpriteEffects.None, 0f);
        return true;
    }

    public bool TryDraw(SpriteBatch sb, string key, Vector2 position, Color tint, float timeSeconds = 0f, SpriteEffects effects = SpriteEffects.None)
    {
        if (!TryResolve(key, out var info, timeSeconds))
            return false;

        sb.Draw(info.Texture, position, info.Source, tint, 0f, info.Origin, info.Scale, effects, 0f);
        return true;
    }

    public (Texture2D Tex, Vector2 Origin) Get(string id)
    {
        if (!TryGet(id, out var tex, out var origin))
            throw new KeyNotFoundException($"Sprite not registered: {id}");
        return (tex, origin);
    }

    public static void DrawMissingPlaceholder(SpriteBatch sb, Texture2D white, Rectangle rect, Color color)
    {
        var fill = color == default ? Color.Magenta : color;
        sb.Draw(white, rect, fill * 0.75f);
        sb.Draw(white, new Rectangle(rect.X, rect.Y, rect.Width, 1), Color.Black * 0.6f);
        sb.Draw(white, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), Color.Black * 0.6f);
        sb.Draw(white, new Rectangle(rect.X, rect.Y, 1, rect.Height), Color.Black * 0.6f);
        sb.Draw(white, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height), Color.Black * 0.6f);
    }

    private void AddSource(SpriteSource source)
    {
        if (source == null || string.IsNullOrWhiteSpace(source.Key) || string.IsNullOrWhiteSpace(source.Path))
            return;

        _sources[source.Key] = source;
    }

    private void ReportMissingExpectedKeys(string label, IEnumerable<string> expectedKeys)
    {
        foreach (var key in expectedKeys)
        {
            if (string.IsNullOrWhiteSpace(key))
                continue;

            if (_sources.ContainsKey(key))
                continue;

            LogMissing($"expected:{label}:{key}", $"missing expected {label} key");
        }
    }

    private void ReportMissingManifestFiles()
    {
        foreach (var source in _sources.Values)
        {
            if (ExistsInAnyRoot(source.Path))
                continue;

            LogMissing($"file:{source.Path}", $"manifest file missing for {source.Key}");
        }
    }

    private bool ExistsInAnyRoot(string relativePath)
        => _primary.Exists(relativePath) || (_fallback?.Exists(relativePath) ?? false);

    private bool TryLoadTexture(string relativePath, out Texture2D texture)
    {
        if (_textureCache.TryGetValue(relativePath, out texture!) && texture != null && !texture.IsDisposed)
            return true;

        if (TryLoadTextureFrom(_primary, relativePath, out texture))
            return true;

        if (_fallback != null && TryLoadTextureFrom(_fallback, relativePath, out texture))
            return true;

        texture = null!;
        LogMissing(relativePath, "missing texture");
        return false;
    }

    private bool TryLoadTextureFrom(RawContent content, string relativePath, out Texture2D texture)
    {
        try
        {
            texture = content.LoadTexture(relativePath);
            _textureCache[relativePath] = texture;
            return true;
        }
        catch (Exception)
        {
            texture = null!;
            return false;
        }
    }

    private static bool TryResolveSourceRect(SpriteSource source, Texture2D texture, float timeSeconds, out Rectangle rect)
    {
        int x = source.X ?? 0;
        int y = source.Y ?? 0;

        int frameWidth = Math.Max(1, source.FrameWidth ?? source.Width ?? texture.Width);
        int frameHeight = Math.Max(1, source.FrameHeight ?? source.Height ?? texture.Height);
        int frameCount = Math.Max(1, source.FrameCount ?? 1);

        int frameIndex = 0;
        if (frameCount > 1 && (source.FrameDuration ?? 0f) > 0f)
            frameIndex = (int)(MathF.Max(0f, timeSeconds) / source.FrameDuration!.Value) % frameCount;

        if (frameCount > 1)
            x += frameIndex * frameWidth;

        rect = new Rectangle(x, y, frameWidth, frameHeight);

        if (rect.X < 0 || rect.Y < 0)
            return false;

        if (rect.Width <= 0 || rect.Height <= 0)
            return false;

        if (rect.Right > texture.Width || rect.Bottom > texture.Height)
            return false;

        // TODO: Detecting fully transparent regions is expensive at runtime; use atlas preview tooling when tuning sheet rectangles.
        return true;
    }

    private static Vector2 ResolveOrigin(SpriteSource source, Rectangle rect)
    {
        if (source.OriginX.HasValue || source.OriginY.HasValue)
        {
            float ox = source.OriginX ?? rect.Width / 2f;
            float oy = source.OriginY ?? (rect.Height - 1);
            return new Vector2(ox, oy);
        }

        return new Vector2(rect.Width / 2f, rect.Height - 1);
    }

    private static float ResolveScale(SpriteSource source)
        => source.Scale.HasValue && source.Scale.Value > 0f ? source.Scale.Value : 1f;

    private void LogMissing(string key, string reason)
    {
        if (!_missingLogged.Add($"{key}|{reason}"))
            return;

        _missingOrder.Add($"{reason}: {key}");
        Debug.WriteLine($"[Assets] {reason}: {key}");
        Console.WriteLine($"[Assets] {reason}: {key}");
    }
}
