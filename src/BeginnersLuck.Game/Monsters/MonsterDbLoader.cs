using System;
using System.Collections.Generic;
using System.Text.Json;
using BeginnersLuck.Game.Stats;

namespace BeginnersLuck.Game.Monsters;

public static class MonsterDbLoader
{
    public static MonsterDb LoadFromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("monsters.json content was empty.", nameof(json));

        var file = JsonSerializer.Deserialize<MonsterDbFileDto>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Failed to deserialize monsters.json.");

        if (file.Version <= 0)
            throw new InvalidOperationException("monsters.json missing/invalid version.");

        var list = new List<MonsterDef>(file.Monsters.Count);

        foreach (var m in file.Monsters)
        {
            var stats = new StatBlock();
            stats[StatType.MaxHp] = Math.Max(1, m.Stats.MaxHp);
            stats[StatType.MaxMp] = Math.Max(0, m.Stats.MaxMp);
            stats[StatType.Atk]   = Math.Max(0, m.Stats.Atk);
            stats[StatType.Def]   = Math.Max(0, m.Stats.Def);
            stats[StatType.Spd]   = Math.Max(0, m.Stats.Spd);

            var def = new MonsterDef
            {
                Id = m.Id?.Trim() ?? "",
                Name = m.Name?.Trim() ?? "",
                SpriteKey = m.SpriteKey?.Trim() ?? "",
                LootTableId = m.LootTableId?.Trim() ?? "",
                Skills = m.Skills ?? Array.Empty<string>(),
                Stats = stats
            };

            def.Validate();
            list.Add(def);
        }

        return new MonsterDb(list);
    }
}
