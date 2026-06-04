using System;
using BeginnersLuck.Game.Items;
using BeginnersLuck.Game.State;
using Microsoft.Xna.Framework;

namespace BeginnersLuck.Game.World;

public static class InteractionSystem
{
    public static GameEntity? FindInteractionTarget(EntityManager entities, Point playerTile, Point facing)
    {
        // 1) On current tile.
        var target = entities.GetEntityAt(playerTile, e => e.IsInteractable && e.IsAlive);
        if (target != null) return target;

        // 2) In front of player.
        var front = playerTile + facing;
        target = entities.GetEntityAt(front, e => e.IsInteractable && e.IsAlive);
        if (target != null) return target;

        // 3) Adjacent fallback if no valid facing target.
        target = entities.GetEntityAt(playerTile + new Point(0, -1), e => e.IsInteractable && e.IsAlive)
                 ?? entities.GetEntityAt(playerTile + new Point(1, 0), e => e.IsInteractable && e.IsAlive)
                 ?? entities.GetEntityAt(playerTile + new Point(0, 1), e => e.IsInteractable && e.IsAlive)
                 ?? entities.GetEntityAt(playerTile + new Point(-1, 0), e => e.IsInteractable && e.IsAlive);

        return target;
    }

    public static bool TryInteract(
        EntityManager entities,
        Point playerTile,
        Point facing,
        CharacterState player,
        ItemDb items,
        Random rng,
        Action<string> log,
        out bool consumesTurn)
    {
        consumesTurn = false;

        var target = FindInteractionTarget(entities, playerTile, facing);
        if (target == null)
            return false;

        switch (target.Type)
        {
            case GameEntityType.ResourceNode:
                if (target.IsDepleted)
                {
                    log($"{target.DisplayName} is depleted.");
                    return true;
                }

                var gathered = SimpleLootGenerator.RollResourceDrop(target, rng);

                target.IsDepleted = true;

                if (player.Inventory.AddItem(gathered.ItemId, gathered.Quantity, items))
                {
                    log($"Gathered {items.DisplayNameOf(gathered.ItemId)} x{gathered.Quantity}.");
                }
                else
                {
                    log($"Could not collect item '{gathered.ItemId}'.");
                }

                entities.RemoveEntity(target);
                consumesTurn = true;
                return true;

            case GameEntityType.Chest:
                if (target.IsOpened)
                {
                    log("The chest is empty.");
                    return true;
                }

                target.IsOpened = true;
                var drops = SimpleLootGenerator.RollChestDrops(rng);

                if (drops.Count == 0)
                {
                    log("You opened a chest. It was empty.");
                }
                else
                {
                    var parts = new string[drops.Count];
                    int n = 0;

                    for (int i = 0; i < drops.Count; i++)
                    {
                        var d = drops[i];
                        if (!player.Inventory.AddItem(d.ItemId, d.Quantity, items))
                            continue;

                        parts[n++] = $"{items.DisplayNameOf(d.ItemId)} x{d.Quantity}";
                    }

                    if (n == 0)
                    {
                        log("You opened a chest. Loot could not be added.");
                    }
                    else
                    {
                        var found = string.Join(", ", parts, 0, n);
                        log($"You opened a chest. Found {found}.");
                    }
                }

                target.BlocksMovement = false;
                consumesTurn = true;
                return true;

            case GameEntityType.Door:
                log(string.IsNullOrWhiteSpace(target.Payload) ? "A doorway leads onward." : target.Payload);
                return true;

            case GameEntityType.Enemy:
                log($"{target.DisplayName} looks hostile.");
                return true;

            default:
                log($"You inspect {target.DisplayName}.");
                return true;
        }
    }
}
