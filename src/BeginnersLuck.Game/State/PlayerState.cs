using System;

namespace BeginnersLuck.Game.State;

public sealed class PlayerState
{
    // --- Core stats ---
    public int Level { get; private set; } = 1;

    public int MaxHp { get; private set; } = 100;
    public int Hp { get; private set; } = 80;

    public int Gold { get; private set; } = 50;

    // XP model: XP inside the current level (0..XpToNextLevel-1)
    public int XpIntoLevel { get; private set; } = 0;

    // Optional: total XP for UI/telemetry
    public int TotalXp { get; private set; } = 0;

    public PlayerInventory Inventory { get; } = new();

    // Last award summary (handy for UI toasts)
    public int LastLevelsGained { get; private set; } = 0;
    public int LastXpGained { get; private set; } = 0;

    // ---------------------------
    // Economy / HP
    // ---------------------------

    public void AddGold(int gold)
    {
        Gold += gold;
        if (Gold < 0) Gold = 0;
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;
        Hp = Math.Min(MaxHp, Hp + amount);
    }

    public void Damage(int amount)
    {
        if (amount <= 0) return;
        Hp = Math.Max(0, Hp - amount);
    }

    // ---------------------------
    // XP / Leveling
    // ---------------------------

    /// <summary>
    /// Existing API: just award XP and level internally.
    /// </summary>
    public void AddXp(int xp)
    {
        AddXp(xp, out _);
    }

    /// <summary>
    /// Award XP and report how many levels were gained.
    /// </summary>
    public void AddXp(int xp, out int levelsGained)
    {
        levelsGained = 0;
        LastLevelsGained = 0;
        LastXpGained = 0;

        if (xp <= 0) return;

        LastXpGained = xp;

        TotalXp += xp;
        XpIntoLevel += xp;

        // Handle multiple level-ups from a big award
        while (XpIntoLevel >= XpToNextLevel())
        {
            XpIntoLevel -= XpToNextLevel();
            LevelUp();
            levelsGained++;
        }

        LastLevelsGained = levelsGained;
    }

    /// <summary>
    /// XP required to go from current Level -> Level+1.
    /// "Fair and balanced" default polynomial:
    /// - fast early (you see progress immediately)
    /// - ramps later (doesn't explode into grind)
    ///
    /// L1: 20
    /// L2: 40
    /// L3: 70
    /// L4: 110
    /// L5: 160
    /// L10: 560
    /// </summary>
    public int XpToNextLevel()
    {
        int l = Math.Max(1, Level);

        // Tune knobs
        const int c = 10; // base
        const int b = 5;  // linear
        const int a = 5;  // quadratic

        int need = c + (b * l) + (a * l * l);
        return Math.Max(1, need);
    }

    /// <summary>
    /// For UI bars: 0..1 progress to next level.
    /// </summary>
    public float XpPercentToNextLevel()
    {
        int need = XpToNextLevel();
        return (need <= 0) ? 0f : Math.Clamp(XpIntoLevel / (float)need, 0f, 1f);
    }

    public void HealToFull()
    {
        Hp = MaxHp;
    }

    // ---------------------------
    // Internals
    // ---------------------------

    private void LevelUp()
    {
        Level++;

        // V1 balance:
        // +5 Max HP per level, and full heal so it feels great immediately.
        MaxHp += 5;
        Hp = MaxHp;
    }
    
}
