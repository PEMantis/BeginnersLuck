using System;

namespace BeginnersLuck.Game.Actors;

public sealed class JobDef
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public StatBlock Base { get; init; } = new();
    public StatBlock Growth { get; init; } = new();
    public string[] StartingSkills { get; init; } = Array.Empty<string>();
}

public sealed class CharacterDef
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string StartingJobId { get; init; } = "";
    public string SpriteKey { get; init; } = "";
}

public sealed class MonsterDef
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public StatBlock Stats { get; init; } = new();
    public string[] Skills { get; init; } = Array.Empty<string>();
    public string SpriteKey { get; init; } = "";
    public string LootTableId { get; init; } = "";
}
