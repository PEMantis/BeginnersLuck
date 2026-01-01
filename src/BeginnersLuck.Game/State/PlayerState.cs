namespace BeginnersLuck.Game.State;

public sealed class PlayerState
{
    public int MaxHp { get; set; } = 100;
    public int Hp { get; set; } = 80;

    public int Gold { get; set; } = 50;

    public int Xp { get; set; } = 0;
    public PlayerInventory Inventory { get; } = new();

    public void AddXp(int xp)
    {
        this.Xp += xp;
    }

    public void AddGold(int gold)
    {
        this.Gold += gold;
    }
}
