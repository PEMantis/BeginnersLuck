namespace BeginnersLuck.Game.Actors;

public sealed class ActorDef
{
    public string Key { get; init; } = "";
    public string Name { get; init; } = "";
    public ActorKind Kind { get; init; }
    public Faction Faction { get; init; }

    // future hooks, but harmless now:
    public string VisualKey { get; init; } = "";   // sprite id or sprite-set id later
}
