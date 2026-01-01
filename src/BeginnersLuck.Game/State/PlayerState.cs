namespace BeginnersLuck.Game.State;

public sealed class PlayerState
{
    public int Xp { get; private set; }
    public int Gold { get; private set; }

    public Inventory Inventory { get; } = new();

    public void AddXp(int amount)
    {
        if (amount <= 0) return;
        Xp += amount;
    }

    public void AddGold(int amount)
    {
        if (amount <= 0) return;
        Gold += amount;
    }
}
