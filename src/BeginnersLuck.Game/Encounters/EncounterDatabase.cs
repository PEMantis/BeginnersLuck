using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace BeginnersLuck.Game.Encounters;

public sealed class EncounterDatabase
{
    private readonly Dictionary<string, EncounterDef> _byId;

    private EncounterDatabase(Dictionary<string, EncounterDef> byId)
        => _byId = byId;

    public EncounterDef Get(string id)
        => _byId.TryGetValue(id, out var e)
            ? e
            : new EncounterDef(id, id, Array.Empty<EncounterEnemyLine>());

    public static EncounterDatabase LoadFromJson(string json)
    {
        var dto = JsonSerializer.Deserialize<DbDto>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new DbDto();

        var dict = new Dictionary<string, EncounterDef>(StringComparer.OrdinalIgnoreCase);

        foreach (var e in dto.Encounters ?? Array.Empty<EncounterDto>())
        {
            if (string.IsNullOrWhiteSpace(e.Id)) continue;

            var enemies = (e.Enemies ?? Array.Empty<EnemyDto>())
                .Where(x => !string.IsNullOrWhiteSpace(x.Id))
                .Select(x => new EncounterEnemyLine(
                    x.Id!, 1))
                .ToArray();

            dict[e.Id!] = new EncounterDef(
                e.Id!,
                string.IsNullOrWhiteSpace(e.Name) ? e.Id! : e.Name!,
                enemies);
        }

        return new EncounterDatabase(dict);
    }

    private sealed class DbDto
    {
        public EncounterDto[]? Encounters { get; set; }
    }

    private sealed class EncounterDto
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public EnemyDto[]? Enemies { get; set; }
    }

    private sealed class EnemyDto
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public int Hp { get; set; }
    }
}
