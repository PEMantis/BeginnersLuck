using System;
using BeginnersLuck.Game.World;

namespace BeginnersLuck.Game.Encounters;

public sealed class BasicEncounterSource : IEncounterSource
{
    public EncounterDef PickEncounter(ZoneInfo zone, Random rng)
    {
        return zone.Id switch
        {
            ZoneId.Grasslands => Roll(rng,
                new EncounterDef("slimes_easy", "Slimes",
                    new[] { new EncounterEnemyLine("slime", 2) }),
                new EncounterDef("goblins_patrol", "Goblin Patrol",
                    new[] { new EncounterEnemyLine("goblin", 3) })
            ),

            ZoneId.Forest => Roll(rng,
                new EncounterDef("wolves", "Wolves",
                    new[] { new EncounterEnemyLine("wolf", 2) }),
                new EncounterDef("bandits", "Bandits",
                    new[] { new EncounterEnemyLine("bandit", 2) })
            ),

            ZoneId.Ruins => new EncounterDef("skeletons", "Skeletons",
                new[] { new EncounterEnemyLine("skeleton", 2) }),

            // ✅ Dev-time guardrail: don't return empty encounters silently.
            _ => throw new InvalidOperationException(
                $"No encounter table for ZoneId={zone.Id}. Add it to BasicEncounterSource or fix ZoneResolver.")
        };
    }

    private static EncounterDef Roll(Random rng, EncounterDef a, EncounterDef b)
        => rng.NextDouble() < 0.5 ? a : b;
}
