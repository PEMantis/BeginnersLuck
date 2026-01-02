using System;

namespace BeginnersLuck.Game.State;

public sealed class PlayerState
{
    public int MaxHp { get; set; } = 100;
    public int Hp { get; set; } = 80;

    public int Gold { get; set; } = 50;

    public int Level { get; private set; } = 1;
    public int Xp { get; private set; } = 0;

    public PlayerInventory Inventory { get; } = new();

    // --- XP curve tuning (nice prototype defaults) ---
    // XP to go from level L -> L+1: A * L^P
    private const int XpA = 60;          // base
    private const double XpP = 2.2;      // growth

    public int XpToNextLevel()
        => XpForLevel(Level);

    public int XpForLevel(int level)
    {
        level = Math.Max(1, level);
        return (int)MathF.Round((float)(XpA * Math.Pow(level, XpP)));
    }

    public float XpProgress01()
    {
        int need = XpToNextLevel();
        if (need <= 0) return 1f;
        return Math.Clamp(Xp / (float)need, 0f, 1f);
    }

    public void AddXp(int xp)
    {
        if (xp <= 0) return;

        Xp += xp;

        // Level up loop
        while (Xp >= XpToNextLevel())
        {
            Xp -= XpToNextLevel();
            LevelUp();
        }
    }

    private void LevelUp()
    {
        Level++;

        // v1: small, sane bumps
        MaxHp += 5;
        Hp = MaxHp; // full heal on level
    }

    public void AddGold(int gold) => Gold += gold;

    public void Heal(int amount) => Hp = Math.Min(MaxHp, Hp + Math.Max(0, amount));

    public void Damage(int amount) => Hp = Math.Max(0, Hp - Math.Max(0, amount));
}
