namespace BeginnersLuck.Game.Graphics;

public sealed record SpriteSource
{
    public string Key { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;

    public int? X { get; init; }
    public int? Y { get; init; }
    public int? Width { get; init; }
    public int? Height { get; init; }

    public int? FrameWidth { get; init; }
    public int? FrameHeight { get; init; }
    public int? FrameCount { get; init; }
    public float? FrameDuration { get; init; }

    public float? Scale { get; init; }
    public float? OriginX { get; init; }
    public float? OriginY { get; init; }

    public string? Notes { get; init; }
}
