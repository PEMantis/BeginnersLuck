using BeginnersLuck.Game.Services;

namespace BeginnersLuck.Game.Items;

public sealed class ItemUseSystem
{
    private readonly GameServices _s;

    public ItemUseSystem(GameServices s) => _s = s;

    /// <summary>
    /// UI helper: can this item be used right now?
    /// </summary>
    public bool CanUse(string itemId, out string reason)
    {
        reason = "";

        if (!_s.Items.TryGet(itemId, out var def))
        {
            reason = "UNKNOWN";
            return false;
        }

        if (!def.Usable || def.Effect == UseEffect.None)
        {
            reason = "NOT USABLE";
            return false;
        }

        if (!_s.Player.Inventory.TryGetCount(itemId, out var qty) || qty <= 0)
        {
            reason = "NONE";
            return false;
        }

        // Add future gating rules here (status effects, battle-only, etc.)
        return true;
    }

    /// <summary>
    /// Uses 1 item if possible, returns a message for toast/UI.
    /// </summary>
    public bool TryUse(string itemId, out string message)
    {
        message = "";

        if (!_s.Items.TryGet(itemId, out var def))
        {
            message = $"Unknown item: {itemId}";
            return false;
        }

        if (!def.Usable || def.Effect == UseEffect.None)
        {
            message = $"{def.Name} cannot be used.";
            return false;
        }

        if (!_s.Player.Inventory.TryGetCount(itemId, out var qty) || qty <= 0)
        {
            message = $"No {def.Name} left.";
            return false;
        }

        switch (def.Effect)
        {
            case UseEffect.HealHp:
            {
                int before = _s.Player.Hp;
                _s.Player.Heal(def.Amount);
                int gained = _s.Player.Hp - before;

                _s.Player.Inventory.Add(itemId, -1);

                message = gained > 0
                    ? $"Used {def.Name}! +{gained} HP"
                    : $"Used {def.Name}! (No effect)";
                return true;
            }

            default:
                message = $"{def.Name} has no effect yet.";
                return false;
        }
    }
}
