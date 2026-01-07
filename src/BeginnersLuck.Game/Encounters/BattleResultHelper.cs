using BeginnersLuck.Game.Services;

namespace BeginnersLuck.Game.Battles;

public static class BattleResultApplier
{
    public static void Apply(GameServices s, BattleResult r)
    {
        if (r.Applied) return;
        r.Applied = true;

        if (r.Outcome != BattleOutcome.Victory)
            return;

        r.XpReport = s.Player.AddXpWithReport(r.Xp);

        s.Player.AddGold(r.Gold);

        foreach (var line in r.Loot)
            s.Player.Inventory.Add(line.ItemId, line.Qty);

        // ✅ Recompute derived stats once after leveling (job growth now matters)
        if (r.XpReport.LevelsGained > 0)
        {
            var stats = s.Stats.ComputeFor(s.Player);
            s.Player.ApplyDerivedStats(stats, healToFull: true);
        }
    }
}
