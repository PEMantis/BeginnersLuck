using System.Linq;
using BeginnersLuck.Game.Services;
using BeginnersLuck.Game.Skills.Adapters;
using BeginnersLuck.Game.Monsters;
using BeginnersLuck.Game.Status;

namespace BeginnersLuck.Game.Skills;

public static class SkillSandbox
{
    public static void Run(GameServices s)
    {
        var statuses = new StatusController();

        var user = new CharacterSkillActor(
            s.Party.Leader,
            s.Stats,
            statuses
        );

        var def = s.Monsters.Get("slime");
        var slime = new MonsterInstance(def);

        var target = new MonsterSkillActor(
            slime,
            statuses
        );

        var res = s.SkillSystem.Resolve(user, "strike", new[] { target });

        foreach (var e in res.Events)
        {
            s.Toasts.Push($"{e.Kind}: {e.Source} -> {e.Target} ({e.Amount})");
        }
    }
}