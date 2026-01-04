using System;

namespace BeginnersLuck.Game.State;

public sealed class TownState
{
    public int Seed { get; set; }

    // Minimal persistence hooks (expand later)
    public bool HasInn { get; set; } = true;
    public bool HasShop { get; set; } = true;

    // Tiny “world reacts to you” proof
    public int TimesRested { get; set; } = 0;
    public int TimesPurchased { get; set; } = 0;

    public TownState(int seed)
    {
        Seed = seed;
    }
}
