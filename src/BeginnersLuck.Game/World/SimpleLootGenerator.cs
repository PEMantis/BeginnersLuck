using System;
using System.Collections.Generic;
using BeginnersLuck.Game.Items;

namespace BeginnersLuck.Game.World;

public static class SimpleLootGenerator
{
    public static ItemStack RollResourceDrop(GameEntity node, Random rng)
    {
        string id = string.IsNullOrWhiteSpace(node.Payload) ? "berries" : node.Payload;

        return id switch
        {
            "berries" => new ItemStack("berries", rng.Next(2, 5)),
            "wood" => new ItemStack("wood", rng.Next(1, 4)),
            "stone" => new ItemStack("stone", rng.Next(1, 3)),
            _ => new ItemStack("berries", rng.Next(1, 3)),
        };
    }

    public static List<ItemStack> RollChestDrops(Random rng)
    {
        var drops = new List<ItemStack>();

        drops.Add(new ItemStack("old_coin", rng.Next(3, 9)));

        if (rng.Next(100) < 65)
            drops.Add(new ItemStack("health_herb", 1));

        if (rng.Next(100) < 55)
            drops.Add(new ItemStack("wood", 2));

        return drops;
    }

    public static List<ItemStack> RollEnemyDrops(GameEntity enemy, Random rng)
    {
        var drops = new List<ItemStack>();

        if (enemy.Type != GameEntityType.Enemy)
            return drops;

        drops.Add(new ItemStack("slime_gel", rng.Next(1, 3)));

        if (rng.Next(100) < 35)
            drops.Add(new ItemStack("old_coin", 1));

        return drops;
    }
}
