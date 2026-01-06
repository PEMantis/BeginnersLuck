using System.Collections.Generic;

namespace BeginnersLuck.Game.State;

public sealed class PartyState
{
    public List<CharacterState> Members { get; } = new();
    public InventoryState Inventory { get; } = new();
    public int Gold { get; set; }
}
