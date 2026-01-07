using System;
using System.Collections.Generic;
using System.Text.Json;

namespace BeginnersLuck.Game.Skills;

public static class SkillDbLoader
{
    private sealed class FileDto
    {
        public int Version { get; set; } = 1;
        public List<SkillDef> Skills { get; set; } = new();
    }

    public static SkillDb LoadFromJson(string json)
    {
        var file = JsonSerializer.Deserialize<FileDto>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Failed to deserialize skills.json.");

        if (file.Version <= 0)
            throw new InvalidOperationException("skills.json missing/invalid version.");

        return new SkillDb(file.Skills);
    }
}
