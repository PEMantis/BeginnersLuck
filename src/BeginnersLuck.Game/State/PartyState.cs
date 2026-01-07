using System;
using System.Collections.Generic;

namespace BeginnersLuck.Game.State;

public sealed class PartyState
{
    public List<CharacterState> Members { get; } = new();

    public CharacterState Leader
    {
        get
        {
            if (Members.Count == 0)
                throw new InvalidOperationException("Party has no members.");
            return Members[0];
        }
    }

    // Shared JRPG bag: for now we point at the leader's inventory so nothing breaks.
    // Later we can move inventory fully onto PartyState and remove it from CharacterState.
    public InventoryState Inventory => Leader.Inventory;

    // Shared gold: for now we point at the leader's gold via methods.
    public int Gold => Leader.Gold;

    public void AddGold(int delta) => Leader.AddGold(delta);
}
