using System;

namespace BeginnersLuck.Game.Services;

public sealed class PlayerXpReport
{
    public int OldLevel { get; init; }
    public int NewLevel { get; init; }
    public int LevelsGained => Math.Max(0, NewLevel - OldLevel);
}
