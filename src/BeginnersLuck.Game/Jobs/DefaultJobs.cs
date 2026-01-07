using BeginnersLuck.Game.Actors;
using BeginnersLuck.Game.Stats;

namespace BeginnersLuck.Game.Jobs;

public static class DefaultJobs
{
    public static JobDb Create()
    {
        var defs = new[]
        {
            new JobDef
            {
                Id = "fighter",
                Name = "Fighter",
                Base = new StatBlock
                {
                    [StatType.MaxHp] = 30,
                    [StatType.MaxMp] = 0,
                    [StatType.Atk]   = 8,
                    [StatType.Def]   = 6,
                    [StatType.Spd]   = 5,
                },
                Growth = new StatBlock
                {
                    [StatType.MaxHp] = 6,
                    [StatType.MaxMp] = 0,
                    [StatType.Atk]   = 2,
                    [StatType.Def]   = 2,
                    [StatType.Spd]   = 1,
                }
            },

            new JobDef
            {
                Id = "rogue",
                Name = "Rogue",
                Base = new StatBlock
                {
                    [StatType.MaxHp] = 26,
                    [StatType.MaxMp] = 0,
                    [StatType.Atk]   = 7,
                    [StatType.Def]   = 4,
                    [StatType.Spd]   = 8,
                },
                Growth = new StatBlock
                {
                    [StatType.MaxHp] = 5,
                    [StatType.MaxMp] = 0,
                    [StatType.Atk]   = 2,
                    [StatType.Def]   = 1,
                    [StatType.Spd]   = 2,
                }
            }
        };

        return new JobDb(defs);
    }
}
