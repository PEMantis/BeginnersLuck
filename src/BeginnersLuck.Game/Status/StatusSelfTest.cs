using System;
using System.Collections.Generic;
using BeginnersLuck.Game.Status;
using BeginnersLuck.Game.Stats;
using BeginnersLuck.Game.Skills.Adapters;
using BeginnersLuck.Game.State;
using BeginnersLuck.Game.Skills;

public static class StatusSelfTest
{
#if DEBUG
    public static void Run(CharacterState c, StatsCalculator stats)
    {
        var statuses = new StatusController();
        var actor = new CharacterSkillActor(c, stats, statuses);

        int atk0 = actor.GetStat(StatId.Atk);

        actor.ApplyTimedStatMod(StatId.Atk, +5, durationTurns: 2);
        int atk1 = actor.GetStat(StatId.Atk);
        if (atk1 != atk0 + 5) throw new Exception("StatusSelfTest: ATK buff not applied.");

        statuses.TickEndOfTurn();
        int atk2 = actor.GetStat(StatId.Atk);
        if (atk2 != atk0 + 5) throw new Exception("StatusSelfTest: ATK buff fell off too early.");

        statuses.TickEndOfTurn();
        int atk3 = actor.GetStat(StatId.Atk);
        if (atk3 != atk0) throw new Exception("StatusSelfTest: ATK buff did not expire.");
    }
#endif
}
