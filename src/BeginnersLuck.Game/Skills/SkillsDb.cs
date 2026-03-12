using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BeginnersLuck.Game.Skills;

public sealed class SkillDb
{
    private readonly Dictionary<string, SkillDef> _byId = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, SkillDef> All => _byId;

    public SkillDef Get(string id)
    {
        if (!_byId.TryGetValue(id, out var def))
            throw new KeyNotFoundException($"Skill '{id}' not found.");
        return def;
    }

    public bool TryGet(string id, out SkillDef? def) => _byId.TryGetValue(id, out def);

    public void LoadFromJson(string json, string? sourceName = null)
    {
        var opts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            Converters = { new JsonStringEnumConverter() }
        };

        var list = JsonSerializer.Deserialize<List<SkillDef>>(json, opts)
                   ?? throw new InvalidOperationException($"Failed to deserialize skills JSON ({sourceName ?? "unknown source"}).");

        _byId.Clear();

        foreach (var s in list)
        {
            if (string.IsNullOrWhiteSpace(s.Id))
                throw new InvalidOperationException($"Skill with missing Id in {sourceName ?? "skills json"}.");

            if (_byId.ContainsKey(s.Id))
                throw new InvalidOperationException($"Duplicate skill id '{s.Id}' in {sourceName ?? "skills json"}.");

            s.Name ??= "";
            s.Description ??= "";
            s.Tags ??= new();
            s.Effects ??= new();

            _byId.Add(s.Id, s);
        }
    }
}
