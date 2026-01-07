using BeginnersLuck.Game.State;

namespace BeginnersLuck.Game.Actors;

public sealed class Actor
{
    public ActorId Id { get; }
    public ActorDef Def { get; }
    public ActorState State { get; }

    public Actor(ActorId id, ActorDef def, ActorState state)
    {
        Id = id;
        Def = def;
        State = state;
    }
}
