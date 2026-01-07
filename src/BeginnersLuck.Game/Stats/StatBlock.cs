using System.Collections.Generic;

namespace BeginnersLuck.Game.Stats;

public enum StatType { MaxHp, MaxMp, Atk, Def, Mag, Res, Spd, Luck }

public sealed class StatBlock
{
    private readonly Dictionary<StatType, int> _v = new();

    public int this[StatType s]
    {
        get => _v.TryGetValue(s, out var x) ? x : 0;
        set => _v[s] = value;
    }

    public void Add(StatBlock other)
    {
        foreach (var kv in other._v)
            this[kv.Key] = this[kv.Key] + kv.Value;
    }

    public StatBlock Clone()
    {
        var b = new StatBlock();
        foreach (var kv in _v) b._v[kv.Key] = kv.Value;
        return b;
    }
}
