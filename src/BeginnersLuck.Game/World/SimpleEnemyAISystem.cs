using System;
using System.Collections.Generic;
using BeginnersLuck.Engine.World;
using BeginnersLuck.Game.State;
using Microsoft.Xna.Framework;

namespace BeginnersLuck.Game.World;

public sealed class SimpleEnemyAISystem
{
    public int AggroRange { get; set; } = 5;

    public void RunTurn(
        EntityManager entities,
        TileMap map,
        Point playerTile,
        CharacterState player,
        Random rng,
        Action<string> log)
    {
        var acting = new List<GameEntity>();
        for (int i = 0; i < entities.Entities.Count; i++)
        {
            var e = entities.Entities[i];
            if (e.Type != GameEntityType.Enemy || !e.IsAlive)
                continue;
            acting.Add(e);
        }

        foreach (var enemy in acting)
        {
            if (!enemy.IsAlive)
                continue;

            int dist = Math.Abs(enemy.Tile.X - playerTile.X) + Math.Abs(enemy.Tile.Y - playerTile.Y);
            if (dist > AggroRange)
                continue;

            if (dist == 1)
            {
                int atk = Math.Max(1, enemy.AttackPower);
                int damage = rng.Next(1, atk + 2);
                player.Damage(damage);
                log($"{enemy.DisplayName} hit you for {damage} damage.");
                continue;
            }

            if (dist > 1)
            {
                var next = ChooseStepToward(enemy.Tile, playerTile, rng);
                if (next == Point.Zero)
                    continue;

                var dest = enemy.Tile + next;
                entities.TryMoveEntity(
                    enemy,
                    dest,
                    isWalkableTile: p => !map.IsSolidCell(p.X, p.Y),
                    blockedByActor: playerTile);
            }
        }
    }

    private static Point ChooseStepToward(Point from, Point to, Random rng)
    {
        int dx = Math.Sign(to.X - from.X);
        int dy = Math.Sign(to.Y - from.Y);

        if (dx == 0 && dy == 0)
            return Point.Zero;

        bool xFirst = Math.Abs(to.X - from.X) >= Math.Abs(to.Y - from.Y);
        if (rng.Next(2) == 0)
            xFirst = !xFirst;

        if (xFirst)
        {
            if (dx != 0) return new Point(dx, 0);
            if (dy != 0) return new Point(0, dy);
        }
        else
        {
            if (dy != 0) return new Point(0, dy);
            if (dx != 0) return new Point(dx, 0);
        }

        return Point.Zero;
    }
}
