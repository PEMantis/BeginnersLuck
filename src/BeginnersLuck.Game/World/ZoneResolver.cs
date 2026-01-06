using Microsoft.Xna.Framework;
using BeginnersLuck.Game.Services;
using BeginnersLuck.WorldGen.Data;
using System;

namespace BeginnersLuck.Game.World;

/// <summary>
/// Decides ZoneInfo for the player's current WORLD tile.
/// Minimal V1: infer zone from terrain/flags that WorldMapScene already knows.
/// </summary>
public static class ZoneResolver
{
   public static ZoneInfo Resolve(GameServices s, Point worldCell)
    => throw new NotSupportedException("ZoneResolver.Resolve requires world tile data. Use ResolveFrom(TileId, TileFlags).");


    /// <summary>
    /// Preferred V1: pass in the world terrain/flags for this cell.
    /// </summary>
    public static ZoneInfo ResolveFrom(TileId terrain, TileFlags flags)
    {
        // Water first
        if (terrain is TileId.Ocean or TileId.DeepWater or TileId.ShallowWater or TileId.Coast)
            return new ZoneInfo(ZoneId.Lake, danger: 0, encounterTableId: "none");

        // Mountains/cliffs
        if (terrain is TileId.Mountain || (flags & TileFlags.Cliff) != 0)
            return new ZoneInfo(ZoneId.Mountains, danger: 3, encounterTableId: "mountain_low");

        // Ruins (if your generator marks ruins via Purpose/flags later)
        // If you already have a flag for ruins, swap it in here.
        // For now: no direct hook, so we don't auto-return Ruins.

        // Roads: low danger but still can have encounters if you want
        if ((flags & TileFlags.Road) != 0)
            return new ZoneInfo(ZoneId.Road, danger: 0, encounterTableId: "road_low");

        // Coasts/beaches: treat as plains
        if ((flags & TileFlags.Coast) != 0 || terrain is TileId.Coast)
            return new ZoneInfo(ZoneId.Plains, danger: 1, encounterTableId: "plains_low");

        // Default land
        // If you have a Grass tile vs Forest tile ID split, map that here.
        // Without that, we do a simple split: rivers -> forest-ish (more danger), else grasslands.
        if ((flags & TileFlags.River) != 0)
            return new ZoneInfo(ZoneId.Forest, danger: 2, encounterTableId: "forest_low");

        return new ZoneInfo(ZoneId.Grasslands, danger: 1, encounterTableId: "plains_low");
    }
}
