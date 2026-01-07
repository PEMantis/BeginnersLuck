namespace BeginnersLuck.Game.Skills;

public sealed class SkillUseReport
{
    public string SkillId { get; init; } = "";
    public string AttackerName { get; init; } = "";
    public string TargetName { get; init; } = "";
    public int Amount { get; init; } // damage or heal
    public bool TargetDowned { get; init; }
}
