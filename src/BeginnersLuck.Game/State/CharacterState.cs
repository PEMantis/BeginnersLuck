using System;
using BeginnersLuck.Game.Services;

namespace BeginnersLuck.Game.State;

public sealed class CharacterState
{
    // --- Identity (JRPG-friendly) ---
    public string Id { get; init; } = "pc_0";
    public string Name { get; set; } = "Hero";
    public string JobId { get; set; } = "fighter";
    // --- Core stats ---
    public int Level { get; private set; } = 1;

    public int MaxHp { get; private set; } = 100;
    public int Hp { get; private set; } = 80;

    // Optional MP now, useful soon (skills). Keep at 0 until you use it.
    public int MaxMp { get; private set; } = 0;
    public int Mp { get; private set; } = 0;

    public int Gold { get; private set; } = 50;

    // XP model: XP inside the current level (0..XpToNextLevel-1)
    public int XpIntoLevel { get; private set; } = 0;

    // Optional: total XP for UI/telemetry
    public int TotalXp { get; private set; } = 0;

    public InventoryState Inventory { get; } = new();

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

    public void HealToFull()
    {
        Hp = MaxHp;
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

    public float XpPercentToNextLevel()
    {
        int need = XpToNextLevel();
        return (need <= 0) ? 0f : Math.Clamp(XpIntoLevel / (float)need, 0f, 1f);
    }

    // ---------------------------
    // Internals
    // ---------------------------

    private void LevelUp()
    {
        Level++;
    }
    public void ApplyDerivedStats(BeginnersLuck.Game.Stats.StatBlock stats, bool healToFull = false)
    {
        int newMaxHp = Math.Max(1, stats[BeginnersLuck.Game.Stats.StatType.MaxHp]);
        int newMaxMp = Math.Max(0, stats[BeginnersLuck.Game.Stats.StatType.MaxMp]);

        // Preserve current % HP when max changes (JRPG-friendly)
        float hpPct = MaxHp <= 0 ? 1f : (Hp / (float)MaxHp);
        float mpPct = MaxMp <= 0 ? 1f : (Mp / (float)MaxMp);

        MaxHp = newMaxHp;
        MaxMp = newMaxMp;

        if (healToFull)
        {
            Hp = MaxHp;
            Mp = MaxMp;
        }
        else
        {
            Hp = Math.Clamp((int)MathF.Round(MaxHp * hpPct), 0, MaxHp);
            Mp = Math.Clamp((int)MathF.Round(MaxMp * mpPct), 0, MaxMp);
        }
    }
    public PlayerXpReport AddXpWithReport(int xp)
    {
        int oldLevel = Level;

        AddXp(xp, out _);

        return new PlayerXpReport
        {
            OldLevel = oldLevel,
            NewLevel = Level
        };
    }


}
