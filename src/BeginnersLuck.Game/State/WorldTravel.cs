namespace BeginnersLuck.Game.State;

public sealed class WorldTravel
{
    // Used by Local -> World resume logic
    public LocalExitResult? PendingLocalExit { get; set; }
}
