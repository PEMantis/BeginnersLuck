using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace BeginnersLuck.Game.World;

public sealed class EntityManager
{
    private readonly List<GameEntity> _entities = new();

    public IReadOnlyList<GameEntity> Entities => _entities;

    public void Clear() => _entities.Clear();

    public bool AddEntity(GameEntity entity)
    {
        if (entity == null) return false;
        if (_entities.Exists(e => e.Id == entity.Id)) return false;

        _entities.Add(entity);
        return true;
    }

    public bool RemoveEntity(GameEntity entity)
    {
        if (entity == null) return false;
        return _entities.Remove(entity);
    }

    public bool RemoveEntity(Guid id)
    {
        int idx = _entities.FindIndex(e => e.Id == id);
        if (idx < 0) return false;

        _entities.RemoveAt(idx);
        return true;
    }

    public GameEntity? GetEntityAt(Point tile, Predicate<GameEntity>? predicate = null)
    {
        for (int i = 0; i < _entities.Count; i++)
        {
            var e = _entities[i];
            if (e.Tile != tile) continue;
            if (predicate != null && !predicate(e)) continue;
            return e;
        }

        return null;
    }

    public List<GameEntity> GetEntitiesNear(Point tile, int radius, Predicate<GameEntity>? predicate = null)
    {
        var outList = new List<GameEntity>();
        int r = Math.Max(0, radius);

        for (int i = 0; i < _entities.Count; i++)
        {
            var e = _entities[i];
            int dist = Math.Abs(e.Tile.X - tile.X) + Math.Abs(e.Tile.Y - tile.Y);
            if (dist > r) continue;
            if (predicate != null && !predicate(e)) continue;
            outList.Add(e);
        }

        return outList;
    }

    public bool IsTileBlocked(Point tile, Guid? ignoreEntityId = null)
    {
        for (int i = 0; i < _entities.Count; i++)
        {
            var e = _entities[i];
            if (ignoreEntityId.HasValue && e.Id == ignoreEntityId.Value) continue;
            if (!e.BlocksMovement) continue;
            if (!e.IsAlive) continue;
            if (e.Tile == tile) return true;
        }

        return false;
    }

    public bool TryMoveEntity(
        GameEntity entity,
        Point next,
        Func<Point, bool> isWalkableTile,
        Point? blockedByActor = null)
    {
        if (entity == null) return false;
        if (!isWalkableTile(next)) return false;
        if (blockedByActor.HasValue && next == blockedByActor.Value) return false;
        if (IsTileBlocked(next, ignoreEntityId: entity.Id)) return false;

        entity.Tile = next;
        return true;
    }
}
