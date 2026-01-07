using System;
using System.Collections.Generic;

namespace BeginnersLuck.Game.Monsters;

public sealed class MonsterDb
{
    private readonly Dictionary<string, MonsterDef> _byId;

    public MonsterDb(IEnumerable<MonsterDef> defs)
    {
        _byId = new Dictionary<string, MonsterDef>(StringComparer.OrdinalIgnoreCase);

        foreach (var d in defs)
        {
            d.Validate();

            if (_byId.ContainsKey(d.Id))
                throw new InvalidOperationException($"Duplicate MonsterDef id '{d.Id}'.");

            _byId.Add(d.Id, d);
        }
    }

    public MonsterDef Get(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Monster id was empty.", nameof(id));

        if (_byId.TryGetValue(id, out var def))
            return def;

        throw new KeyNotFoundException($"MonsterDef not found: '{id}'");
    }

    public bool TryGet(string id, out MonsterDef def)
        => _byId.TryGetValue(id, out def!);
}
