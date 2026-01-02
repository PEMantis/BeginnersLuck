using System;

namespace BeginnersLuck.Game.State;

public sealed class PlayerState
{
    public int Level { get; private set; } = 1;

    public int MaxHp { get; set; } = 100;
    public int Hp { get; set; } = 80;

    public int Gold { get; set; } = 50;

    public int Xp { get; private set; } = 0;
    public PlayerInventory Inventory { get; } = new();

    // --- XP CURVE (tune these) ---
    private const int BaseXp = 30;     // XP needed from Lv1->Lv2 baseline
    private const int Growth = 10;     // extra growth per level^2 (quadratic)

    public int XpToNextLevel()
    {
        // XP required to go from current Level to Level+1
        // Example: L1: 30+10*1 = 40, L2: 30+10*4 = 70, L3: 30+10*9 = 120 ...
        return BaseXp + Growth * (Level * Level);
    }

    public int XpIntoLevel()
        => Xp - TotalXpForLevel(Level);

    public int TotalXpForLevel(int level)
    {
        // Total XP required to *reach* "level" (level 1 => 0 XP)
        // Sum_{k=1..level-1} (BaseXp + Growth*k^2)
        if (level <= 1) return 0;

        int total = 0;
        for (int k = 1; k <= level - 1; k++)
            total += BaseXp + Growth * (k * k);

        return total;
    }

    public int TotalXpForNextLevel()
        => TotalXpForLevel(Level + 1);

    public int AddXp(int amount)
    {
        if (amount <= 0) return 0;

        Xp += amount;

        int levelsGained = 0;
        while (Xp >= TotalXpForNextLevel())
        {
            Level++;
            levelsGained++;
            OnLevelUp();
        }

        return levelsGained;
    }

    private void OnLevelUp()
    {
        // v1: simple progression
        MaxHp += 5;
        Hp = Math.Min(MaxHp, Hp + 5); // little heal bump
        // later: stats, skill points, etc.
    }

    public void AddGold(int gold) => Gold += gold;

    public void Heal(int amount) => Hp = Math.Min(MaxHp, Hp + amount);
    public void Damage(int amount) => Hp = Math.Max(0, Hp - amount);
}
