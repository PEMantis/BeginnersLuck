using System;
using BeginnersLuck.Game.Items;
using BeginnersLuck.Game.State;
using Microsoft.Xna.Framework;

namespace BeginnersLuck.Game.World;

public sealed class LocalMapCombatSystem
{
    public bool TryPlayerAttack(
        EntityManager entities,
        Point playerTile,
        Point facing,
        CharacterState player,
        ItemDb items,
        Random rng,
        Action<string> log)
    {
        var target = FindAdjacentEnemy(entities, playerTile, facing);
        if (target == null)
            return false;

        int playerDamage = rng.Next(2, 6);
        int dealt = target.Damage(playerDamage);

        if (dealt <= 0)
            dealt = 1;

        log($"You hit {target.DisplayName} for {dealt} damage.");

        if (!target.IsAlive)
        {
            entities.RemoveEntity(target);
            log($"{target.DisplayName} died.");

            int xp = rng.Next(2, 5);
            player.AddXp(xp);
            log($"Gained {xp} XP.");

            var drops = SimpleLootGenerator.RollEnemyDrops(target, rng);
            for (int i = 0; i < drops.Count; i++)
            {
                var d = drops[i];
                if (!player.Inventory.AddItem(d.ItemId, d.Quantity, items))
                    continue;

                log($"Loot: {items.DisplayNameOf(d.ItemId)} x{d.Quantity}.");
            }
        }

        return true;
    }

    public static GameEntity? FindAdjacentEnemy(EntityManager entities, Point playerTile, Point facing)
    {
        var front = playerTile + facing;
        var enemy = entities.GetEntityAt(front, e => e.Type == GameEntityType.Enemy && e.IsAlive);
        if (enemy != null) return enemy;

        enemy = entities.GetEntityAt(playerTile + new Point(0, -1), e => e.Type == GameEntityType.Enemy && e.IsAlive)
                ?? entities.GetEntityAt(playerTile + new Point(1, 0), e => e.Type == GameEntityType.Enemy && e.IsAlive)
                ?? entities.GetEntityAt(playerTile + new Point(0, 1), e => e.Type == GameEntityType.Enemy && e.IsAlive)
                ?? entities.GetEntityAt(playerTile + new Point(-1, 0), e => e.Type == GameEntityType.Enemy && e.IsAlive);

        return enemy;
    }
}
