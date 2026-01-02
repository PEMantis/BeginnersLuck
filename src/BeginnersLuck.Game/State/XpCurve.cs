using System;

namespace BeginnersLuck.Game.State;

public static class XpCurve
{
    // Tune these live until it feels right.
    // A = overall pace. P = steepness.
    public const double A = 50.0;
    public const double P = 2.2;

    // Total XP required to *reach* level L (L>=1).
    // Level 1 is always 0.
    public static int TotalForLevel(int level)
    {
        if (level <= 1) return 0;

        double x = level - 1;
        double total = A * Math.Pow(x, P);

        // Safe rounding behavior: floor keeps thresholds stable.
        return (int)Math.Floor(total);
    }

    // XP needed to go from level -> level+1
    public static int ToNext(int level)
        => Math.Max(1, TotalForLevel(level + 1) - TotalForLevel(level));

    // Derive level from total XP
    public static int LevelFromXp(int xp)
    {
        if (xp <= 0) return 1;

        // Fast enough for small levels. If you later expect 200+ levels,
        // we can binary search instead.
        int level = 1;
        while (TotalForLevel(level + 1) <= xp)
            level++;

        return level;
    }

    public static (int level, int xpIntoLevel, int xpThisLevel) Progress(int xp)
    {
        int level = LevelFromXp(xp);
        int baseXp = TotalForLevel(level);
        int nextXp = TotalForLevel(level + 1);

        int into = xp - baseXp;
        int span = Math.Max(1, nextXp - baseXp);

        return (level, into, span);
    }
}
