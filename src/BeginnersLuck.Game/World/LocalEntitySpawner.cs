using System;
using System.Collections.Generic;
using BeginnersLuck.Engine.World;
using BeginnersLuck.Game.Assets;
using Microsoft.Xna.Framework;

namespace BeginnersLuck.Game.World;

public static class LocalEntitySpawner
{
    public static void SpawnDefaults(
        EntityManager entities,
        TileMap map,
        Point playerTile,
        Point? townCenter,
        int seed)
    {
        var rng = new Random(seed ^ 0x5A17);
        var reserved = new HashSet<Point> { playerTile };
        if (townCenter.HasValue) reserved.Add(townCenter.Value);

        Point? FindOpen(int minDistFromPlayer = 2)
        {
            for (int tries = 0; tries < 500; tries++)
            {
                int x = rng.Next(1, Math.Max(2, map.Width - 1));
                int y = rng.Next(1, Math.Max(2, map.Height - 1));
                var p = new Point(x, y);

                if (reserved.Contains(p)) continue;
                if (map.IsSolidCell(x, y)) continue;

                int dist = Math.Abs(p.X - playerTile.X) + Math.Abs(p.Y - playerTile.Y);
                if (dist < minDistFromPlayer) continue;

                reserved.Add(p);
                return p;
            }

            return null;
        }

        var resourceA = FindOpen();
        if (resourceA.HasValue)
        {
            entities.AddEntity(new GameEntity("Berry Bush", resourceA.Value, GameEntityType.ResourceNode, blocksMovement: false)
            {
                RenderColor = new Color(88, 180, 80),
                SpriteId = AssetKeys.Entities.BerryBush,
                Payload = "berries",
            });
        }

        var resourceB = FindOpen();
        if (resourceB.HasValue)
        {
            bool woodNode = rng.Next(2) == 0;

            entities.AddEntity(new GameEntity(
                woodNode ? "Fallen Branches" : "Loose Stones",
                resourceB.Value,
                GameEntityType.ResourceNode,
                blocksMovement: false)
            {
                RenderColor = new Color(70, 160, 100),
                SpriteId = woodNode ? AssetKeys.Entities.WoodPile : AssetKeys.Entities.StonePile,
                Payload = woodNode ? "wood" : "stone",
            });
        }

        var chest = FindOpen();
        if (chest.HasValue)
        {
            entities.AddEntity(new GameEntity("Old Chest", chest.Value, GameEntityType.Chest, blocksMovement: true)
            {
                RenderColor = new Color(166, 120, 66),
                SpriteId = AssetKeys.Entities.OldChest,
            });
        }

        var enemy = FindOpen(minDistFromPlayer: 4);
        if (enemy.HasValue)
        {
            entities.AddEntity(new GameEntity("Weak Slime", enemy.Value, GameEntityType.Enemy, blocksMovement: true)
            {
                RenderColor = new Color(102, 222, 138),
                SpriteId = AssetKeys.Entities.Slime,
                MaxHp = 8,
                Hp = 8,
                AttackPower = 2,
            });
        }

        var door = FindEdgeDoor(map, playerTile, reserved);
        if (door.HasValue)
        {
            entities.AddEntity(new GameEntity("Gate", door.Value, GameEntityType.Door, blocksMovement: false)
            {
                RenderColor = new Color(80, 140, 210),
                SpriteId = AssetKeys.Entities.Gate,
                Payload = "A marked exit. Walk to map edge to leave the area.",
            });
        }
    }

    private static Point? FindEdgeDoor(TileMap map, Point playerTile, HashSet<Point> reserved)
    {
        Point[] candidates =
        {
            new Point(1, map.Height / 2),
            new Point(map.Width - 2, map.Height / 2),
            new Point(map.Width / 2, 1),
            new Point(map.Width / 2, map.Height - 2),
        };

        Point? best = null;
        int bestDist = int.MinValue;

        for (int i = 0; i < candidates.Length; i++)
        {
            var p = candidates[i];
            if ((uint)p.X >= (uint)map.Width || (uint)p.Y >= (uint)map.Height) continue;
            if (map.IsSolidCell(p.X, p.Y)) continue;
            if (reserved.Contains(p)) continue;

            int dist = Math.Abs(p.X - playerTile.X) + Math.Abs(p.Y - playerTile.Y);
            if (dist > bestDist)
            {
                best = p;
                bestDist = dist;
            }
        }

        if (best.HasValue)
            reserved.Add(best.Value);

        return best;
    }
}
