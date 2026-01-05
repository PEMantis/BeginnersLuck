using BeginnersLuck.Game.Services;

namespace BeginnersLuck.Game.Battles;

public static class BattleResultApplier
{
    public static void Apply(GameServices s, BattleResult r)
    {
        if (r.Applied) return;
        r.Applied = true;

        // Only apply rewards on victory (or adjust if you want partial rewards on flee later)
        if (r.Outcome != BattleOutcome.Victory)
            return;

        r.XpReport = s.Player.AddXpWithReport(r.Xp);
        s.Player.AddGold(r.Gold);

        foreach (var line in r.Loot)
            s.Player.Inventory.Add(line.ItemId, line.Qty);
    }
}
