using System;
using System.Collections.Generic;

public sealed class MonsterDbFile
{
    public int Version { get; set; } = 1;
    public List<MonsterDefDto> Monsters { get; set; } = new();
}

public sealed class MonsterDefDto
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string SpriteKey { get; set; } = "";
    public StatsDto Stats { get; set; } = new();

    public string[] StartingSkills { get; set; } = Array.Empty<string>();

    public EquipmentDto? Equipment { get; set; }
    public InventoryLineDto[] Inventory { get; set; } = Array.Empty<InventoryLineDto>();
}

public sealed class EquipmentDto
{
    public string? Weapon { get; set; }
}

public readonly record struct InventoryLineDto(string Id, int Qty);
