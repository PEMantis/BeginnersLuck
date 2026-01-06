using System;
using BeginnersLuck.Game.Actors;
using BeginnersLuck.Game.Items;
using BeginnersLuck.Game.Jobs;

namespace BeginnersLuck.Game.State;

public sealed class StatsCalculator
{
    private readonly JobDb _jobs;
    private readonly ItemDb _items;

    public StatsCalculator(JobDb jobs, ItemDb items)
    {
        _jobs = jobs;
        _items = items;
    }

    public StatBlock Compute(CharacterState c, CharacterDef cdef)
    {
        var job = _jobs.Get(c.Id);
        var total = job.Base.Clone();

        // Apply growth (Level 1 = base only)
        int levelsGained = Math.Max(0, c.Level - 1);
        if (levelsGained > 0)
        {
            var growth = job.Growth;
            foreach (StatType s in Enum.GetValues(typeof(StatType)))
                total[s] = total[s] + growth[s] * levelsGained;
        }

        // Apply equipment bonuses when this becomes relevant 
        // total.Add(itemBonuses)

        return total;
    }
}
