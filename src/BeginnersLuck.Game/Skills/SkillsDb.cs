using System;
using System.Collections.Generic;

namespace BeginnersLuck.Game.Skills;

public sealed class SkillDb
{
    private readonly Dictionary<string, SkillDef> _byId;

    public SkillDb(IEnumerable<SkillDef> defs)
    {
        _byId = new Dictionary<string, SkillDef>(StringComparer.OrdinalIgnoreCase);

        foreach (var d in defs)
        {
            d.Validate();

            if (_byId.ContainsKey(d.Id))
                throw new InvalidOperationException($"Duplicate SkillDef id '{d.Id}'.");

            _byId.Add(d.Id, d);
        }
    }

    public SkillDef Get(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Skill id was empty.", nameof(id));

        if (_byId.TryGetValue(id, out var def))
            return def;

        throw new KeyNotFoundException($"SkillDef not found: '{id}'");
    }

    public bool TryGet(string id, out SkillDef def)
        => _byId.TryGetValue(id, out def!);
}
