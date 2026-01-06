using System.Collections.Generic;

namespace BeginnersLuck.Game.State;
public enum EquipmentSlot { Weapon, Head, Body, Accessory1, Accessory2 }

public sealed class EquipmentState
{
    private readonly Dictionary<EquipmentSlot, string?> _slotToItemId = new();

    public string? Get(EquipmentSlot slot) => _slotToItemId.TryGetValue(slot, out var v) ? v : null;
    public void Set(EquipmentSlot slot, string? itemId) => _slotToItemId[slot] = itemId;
}
