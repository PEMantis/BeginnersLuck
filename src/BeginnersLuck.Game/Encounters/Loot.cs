namespace BeginnersLuck.Game.Encounters;

public readonly record struct LootDrop(string ItemId, int ChancePercent, int MinQty = 1, int MaxQty = 1);

public readonly record struct RewardRange(int Min, int Max)
{
    public int Roll(System.Random rng)
    {
        if (Max < Min) return Min;
        return rng.Next(Min, Max + 1);
    }
}
