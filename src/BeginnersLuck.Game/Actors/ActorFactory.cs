using System;
using BeginnersLuck.Game.Encounters;
using BeginnersLuck.Game.State;

namespace BeginnersLuck.Game.Actors;

public sealed class ActorFactory
{
    private int _nextId = 1;

    public Actor CreatePlayer(PlayerState player)
    {
        // For now, just mirror HP values.
        // Later we can have ActorState forward to PlayerState instead of copying.
        var def = new ActorDef
        {
            Key = "player",
            Name = "HERO",
            Kind = ActorKind.Player,
            Faction = Faction.Player,
            VisualKey = "battle.party.pawn"
        };

        var a = new Actor(new ActorId(_nextId++), def, player.MaxHp);
        a.State.Hp = player.Hp;
        return a;
    }

    public Actor CreateEnemy(EnemyDef e)
    {
        var def = new ActorDef
        {
            Key = $"monster.{e.Id}",
            Name = string.IsNullOrWhiteSpace(e.Name) ? e.Id : e.Name,
            Kind = ActorKind.Monster,
            Faction = Faction.Hostile,
            VisualKey = $"battle.enemy.{e.Id}"
        };

        var a = new Actor(new ActorId(_nextId++), def, maxHp: Math.Max(1, e.Hp));
        a.State.Hp = Math.Clamp(e.Hp, 0, a.State.MaxHp);
        return a;
    }
}
