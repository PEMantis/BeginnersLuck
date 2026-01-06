using BeginnersLuck.Game.State;

namespace BeginnersLuck.Game.Actors;

public sealed class Actor
{
    public ActorId Id { get; }
    public ActorDef Def { get; }
    public ActorState State { get; }

    public Actor(ActorId id, ActorDef def, int maxHp)
    {
        Id = id;
        Def = def;
        State = new ActorState { MaxHp = maxHp, Hp = maxHp };
    }
}
