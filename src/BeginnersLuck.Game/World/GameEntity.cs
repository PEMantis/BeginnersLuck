using System;
using Microsoft.Xna.Framework;

namespace BeginnersLuck.Game.World;

public sealed class GameEntity
{
    public Guid Id { get; } = Guid.NewGuid();

    public string DisplayName { get; set; }

    public Point Tile { get; set; }

    public GameEntityType Type { get; set; }

    public bool BlocksMovement { get; set; }

    public string? SpriteId { get; set; }

    // Placeholder render style when no sprite is available.
    public Color RenderColor { get; set; } = Color.White;

    public int MaxHp { get; set; }

    public int Hp { get; set; }

    public int AttackPower { get; set; }

    public bool IsInteractable { get; set; } = true;

    // Generic runtime state for simple object interactions.
    public bool IsOpened { get; set; }
    public bool IsDepleted { get; set; }
    public string Payload { get; set; } = string.Empty;

    public bool IsAlive => MaxHp <= 0 || Hp > 0;

    public GameEntity(
        string displayName,
        Point tile,
        GameEntityType type,
        bool blocksMovement)
    {
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? "Entity" : displayName;
        Tile = tile;
        Type = type;
        BlocksMovement = blocksMovement;
    }

    public int Damage(int amount)
    {
        if (amount <= 0 || MaxHp <= 0)
            return 0;

        int before = Hp;
        Hp = Math.Max(0, Hp - amount);
        return before - Hp;
    }
}
