using System.Collections.Generic;
using BeginnersLuck.WorldGen;

namespace BeginnersLuck.Engine.World;

public sealed class LocalMapMeta
{
    public Cell? TownCenterCell { get; set; }

    // key can be "portal:ancient_gate", "dungeon:001", etc.
    public Dictionary<string, Cell> PortalAnchors { get; } = new();

    // Optional now; very useful soon
    public HashSet<int> RoadTileIds { get; } = new();
}
