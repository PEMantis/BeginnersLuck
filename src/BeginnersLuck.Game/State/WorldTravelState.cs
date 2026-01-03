namespace BeginnersLuck.Game.State;

/// <summary>
/// Small, explicit travel mailbox for scene-to-scene handoffs.
/// Keeps WorldState clean as it grows.
/// </summary>
public sealed class WorldTravelState
{
    /// <summary>
    /// Set by LocalMapScene when you walk off an edge.
    /// Consumed by WorldMapScene on resume (then cleared).
    /// </summary>
    public LocalExitResult? PendingLocalExit { get; set; }
}
