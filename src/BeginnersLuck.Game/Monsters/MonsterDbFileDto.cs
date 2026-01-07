using System;
using System.Collections.Generic;

namespace BeginnersLuck.Game.Monsters;

public sealed class MonsterDbFileDto
{
    public int Version { get; set; } = 1;
    public List<MonsterDefDto> Monsters { get; set; } = new();
}

public sealed class MonsterDefDto
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string SpriteKey { get; set; } = "";
    public string LootTableId { get; set; } = "";
    public string[] Skills { get; set; } = Array.Empty<string>();
    public MonsterStatsDto Stats { get; set; } = new();
}

public sealed class MonsterStatsDto
{
    public int MaxHp { get; set; }
    public int MaxMp { get; set; }
    public int Atk { get; set; }
    public int Def { get; set; }
    public int Spd { get; set; }
}
