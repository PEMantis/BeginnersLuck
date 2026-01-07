using System.Linq;
using BeginnersLuck.Game.Services;
using BeginnersLuck.Game.Skills.Adapters;

namespace BeginnersLuck.Game.Skills;

public static class SkillSandbox
{
    public static void Run(GameServices s)
    {
        var user = new CharacterSkillActor(s.Party.Leader, s.Stats);

        // pick first monster def and create an instance the way your game does
        // This is pseudo until we match your monster runtime type.
        var slime = s.Monsters.CreateInstance("slime", level: user.Level); // adjust
        var target = new MonsterSkillActor(slime);

        var res = s.SkillSystem.Resolve(user, "strike", new[] { target });

        // Log results without touching battle UI:
        // toast + maybe console
        foreach (var e in res.Events)
            s.Toasts.Add($"{e.Kind}: {e.Source} -> {e.Target} ({e.Amount})");
    }
}
