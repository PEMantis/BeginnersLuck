using System;
using BeginnersLuck.Game.World;

namespace BeginnersLuck.Game.Encounters;

public sealed class BasicEncounterSource : IEncounterSource
{
    public EncounterDef PickEncounter(ZoneInfo zone, Random rng)
    {
        // Totally runtime. Replace later with procedural tables, AI, whatever.
        return zone.Id switch
        {
            ZoneId.Grasslands => Roll(rng,
                new EncounterDef("slimes_easy", "Slimes",
                    new[] { new EnemyDef("slime", "Slime", 12), new EnemyDef("slime", "Slime", 12) }),
                new EncounterDef("goblins_patrol", "Goblin Patrol",
                    new[] { new EnemyDef("goblin", "Goblin", 10), new EnemyDef("goblin", "Goblin", 10), new EnemyDef("goblin", "Goblin", 10) })
            ),

            ZoneId.Forest => Roll(rng,
                new EncounterDef("wolves", "Wolves",
                    new[] { new EnemyDef("wolf", "Wolf", 14), new EnemyDef("wolf", "Wolf", 14) }),
                new EncounterDef("bandits", "Bandits",
                    new[] { new EnemyDef("bandit", "Bandit", 16), new EnemyDef("bandit", "Bandit", 16) })
            ),

            ZoneId.Ruins => new EncounterDef("skeletons", "Skeletons",
                new[] { new EnemyDef("skeleton", "Skeleton", 18), new EnemyDef("skeleton", "Skeleton", 18) }),

            _ => new EncounterDef("nothing", "Nothing Here", Array.Empty<EnemyDef>())
        };
    }

    private static EncounterDef Roll(Random rng, EncounterDef a, EncounterDef b)
        => rng.NextDouble() < 0.5 ? a : b;
}
