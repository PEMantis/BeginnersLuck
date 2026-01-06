namespace BeginnersLuck.Game.State;
public sealed class ActorState
{
    public int Hp;
    public int MaxHp;

    public int Xp;
    public int Level;

    public bool IsDead => Hp <= 0;
}
