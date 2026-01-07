using System;

namespace BeginnersLuck.Game.Skills;

/// <summary>
/// Adapter interface so SkillSystem can operate without depending on BattleScene/UI.
/// Later, you can make your battle Actor/Unit implement this directly, or wrap it.
/// </summary>
public interface ISkillActor
{
    string DebugName { get; }

    int Level { get; }

    int Hp { get; }
    int HpMax { get; }
    int Mp { get; }
    int MpMax { get; }

    int GetStat(StatId stat);

    void SpendHp(int amount);
    void SpendMp(int amount);

    void TakeDamage(int amount);
    void Heal(int amount);

    // For now: stubs are fine. You can hook these to your real status/buff system later.
    void ApplyTimedStatMod(StatId stat, int delta, int durationTurns);
}
