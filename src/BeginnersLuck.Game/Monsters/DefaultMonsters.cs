using BeginnersLuck.Game.Stats;

namespace BeginnersLuck.Game.Monsters;

public static class DefaultMonsters
{
    public static MonsterDb Create()
    {
        var defs = new[]
        {
            new MonsterDef
            {
                Id = "slime",
                Name = "Slime",
                SpriteKey = "battle.slime",
                Stats = new StatBlock
                {
                    [StatType.MaxHp] = 12,
                    [StatType.Atk] = 3,
                    [StatType.Def] = 1,
                    [StatType.Spd] = 2,
                }
            },
            new MonsterDef
            {
                Id = "goblin",
                Name = "Goblin",
                SpriteKey = "battle.goblin",
                Stats = new StatBlock
                {
                    [StatType.MaxHp] = 10,
                    [StatType.Atk] = 4,
                    [StatType.Def] = 2,
                    [StatType.Spd] = 4,
                }
            },
            new MonsterDef
            {
                Id = "wolf",
                Name = "Wolf",
                SpriteKey = "battle.wolf",
                Stats = new StatBlock
                {
                    [StatType.MaxHp] = 14,
                    [StatType.Atk] = 5,
                    [StatType.Def] = 2,
                    [StatType.Spd] = 6,
                }
            },
            new MonsterDef
            {
                Id = "bandit",
                Name = "Bandit",
                SpriteKey = "battle.bandit",
                Stats = new StatBlock
                {
                    [StatType.MaxHp] = 16,
                    [StatType.Atk] = 6,
                    [StatType.Def] = 3,
                    [StatType.Spd] = 5,
                }
            },
            new MonsterDef
            {
                Id = "skeleton",
                Name = "Skeleton",
                SpriteKey = "battle.skeleton",
                Stats = new StatBlock
                {
                    [StatType.MaxHp] = 18,
                    [StatType.Atk] = 6,
                    [StatType.Def] = 4,
                    [StatType.Spd] = 3,
                }
            },
        };

        return new MonsterDb(defs);
    }
}
