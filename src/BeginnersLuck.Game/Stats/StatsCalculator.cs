using System;
using BeginnersLuck.Game.Jobs;
using BeginnersLuck.Game.Items;
using BeginnersLuck.Game.State;

namespace BeginnersLuck.Game.Stats;

public sealed class StatsCalculator
{
    private readonly JobDb _jobs;
    private readonly ItemDb _items;

    public StatsCalculator(JobDb jobs, ItemDb items)
    {
        _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        _items = items ?? throw new ArgumentNullException(nameof(items));
    }

    public StatBlock ComputeFor(CharacterState c)
    {
        var job = _jobs.Get(c.JobId);

        var total = job.Base.Clone();

        // growth for levels gained (level 1 = base only)
        int gained = Math.Max(0, c.Level - 1);

        foreach (StatType s in Enum.GetValues(typeof(StatType)))
            total[s] = total[s] + job.Growth[s] * gained;

        // Equipment bonuses later:
        // foreach equipped itemId -> add its stat mods

        return total;
    }

    public StatBlock ComputeDerived(StatBlock baseStats)
    {
        // V1: no equipment/buffs yet: derived = base
        var d = new StatBlock();
        d.CopyFrom(baseStats);
        return d;
    }
}
