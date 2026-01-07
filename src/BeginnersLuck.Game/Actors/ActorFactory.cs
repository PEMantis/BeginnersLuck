using BeginnersLuck.Game.Jobs;
using BeginnersLuck.Game.Monsters;
using BeginnersLuck.Game.State;
using BeginnersLuck.Game.Stats;

namespace BeginnersLuck.Game.Actors;

public sealed class ActorFactory
{
    private readonly JobDb _jobs;
    private readonly MonsterDb _monsters;
    private readonly StatsCalculator _stats;

    private int _nextId = 1;

    public ActorFactory(JobDb jobs, MonsterDb monsters, StatsCalculator stats)
    {
        _jobs = jobs;
        _monsters = monsters;
        _stats = stats;
    }

    public Actor CreateMonster(string monsterId, int level = 1)
    {
        var def = _monsters.Get(monsterId);

        var actorDef = new ActorDef
        {
            Key = def.Id,
            Name = def.Name,
            Kind = ActorKind.Monster,
            Faction = Faction.Hostile,
            VisualKey = def.SpriteKey
        };

        var state = new ActorState
        {
            Level = level,
            Skills = def.Skills
        };

        state.BaseStats.CopyFrom(def.Stats);
        state.Mp = state.MaxMp;

        return new Actor(new ActorId(_nextId++), actorDef, state);
    }

    public Actor CreatePlayer(CharacterDef cdef)
    {
        var job = _jobs.Get(cdef.StartingJobId);

        var actorDef = new ActorDef
        {
            Key = cdef.Id,
            Name = cdef.Name,
            Kind = ActorKind.Player,
            Faction = Faction.Player,
            VisualKey = cdef.SpriteKey
        };

        var state = new ActorState
        {
            Level = 1,
            Skills = job.StartingSkills
        };

        state.BaseStats.CopyFrom(job.Base);
        state.Hp = state.MaxHp;
 

        return new Actor(new ActorId(_nextId++), actorDef, state);
    }
}
