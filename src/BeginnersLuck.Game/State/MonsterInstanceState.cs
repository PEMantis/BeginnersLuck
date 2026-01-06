namespace BeginnersLuck.Game.State;
public sealed class MonsterInstanceState
{
    public string MonsterDefId { get; init; } = "";
    public int Hp { get; set; }
    public int Mp { get; set; }
}