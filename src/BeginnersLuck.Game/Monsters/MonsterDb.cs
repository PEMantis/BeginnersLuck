using System;
using System.Collections.Generic;

namespace BeginnersLuck.Game.Monsters;

public sealed class MonsterDb
{
    private readonly Dictionary<string, MonsterDef> _byId = new(StringComparer.OrdinalIgnoreCase);

    public MonsterDb(IEnumerable<MonsterDef> defs)
    {
        foreach (var d in defs)
        {
            d.Validate();
            _byId[d.Id] = d;
        }
    }

    public MonsterDef Get(string id)
    {
        if (!_byId.TryGetValue(id, out var d))
            throw new KeyNotFoundException($"MonsterDef not found: '{id}'");

        return d;
    }

    public bool TryGet(string id, out MonsterDef def) => _byId.TryGetValue(id, out def!);

    public IEnumerable<MonsterDef> All => _byId.Values;
}
