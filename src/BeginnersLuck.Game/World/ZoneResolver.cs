using Microsoft.Xna.Framework;
using BeginnersLuck.Game.Services;
using BeginnersLuck.WorldGen.Data;

namespace BeginnersLuck.Game.World;

/// <summary>
/// Decides ZoneInfo for the player's current WORLD tile.
/// Minimal V1: infer zone from terrain/flags that WorldMapScene already knows.
/// </summary>
public static class ZoneResolver
{
    public static ZoneInfo Resolve(GameServices s, Point worldCell)
    {
        // We need world tile data. WorldMapScene already has it, but we don't want
        // to reach into scene fields. So for V1 we store it in s.World (small cache),
        // OR we use a fallback simple zone.
        //
        // Since you already have _terrainFlat/_flagsFlat in WorldMapScene,
        // the simplest path is: WorldMapScene calls ResolveFrom(...) overload.
        return new ZoneInfo(ZoneId.Grasslands, Danger: 0, EncounterTableId: "plains_low");
    }

    /// <summary>
    /// Preferred V1: pass in the world terrain/flags for this cell.
    /// </summary>
    public static ZoneInfo ResolveFrom(TileId terrain, TileFlags flags)
    {
        // Water first
        if (terrain is TileId.Ocean or TileId.DeepWater or TileId.ShallowWater or TileId.Coast)
            return new ZoneInfo(ZoneId.Lake, Danger: 0, EncounterTableId: "none");

        // Mountains/cliffs
        if (terrain is TileId.Mountain || (flags & TileFlags.Cliff) != 0)
            return new ZoneInfo(ZoneId.Mountains, Danger: 3, EncounterTableId: "mountain_low");

        // Ruins (if your generator marks ruins via Purpose/flags later)
        // If you already have a flag for ruins, swap it in here.
        // For now: no direct hook, so we don't auto-return Ruins.

        // Roads: low danger but still can have encounters if you want
        if ((flags & TileFlags.Road) != 0)
            return new ZoneInfo(ZoneId.Road, Danger: 0, EncounterTableId: "road_low");

        // Coasts/beaches: treat as plains
        if ((flags & TileFlags.Coast) != 0 || terrain is TileId.Coast)
            return new ZoneInfo(ZoneId.Plains, Danger: 1, EncounterTableId: "plains_low");

        // Default land
        // If you have a Grass tile vs Forest tile ID split, map that here.
        // Without that, we do a simple split: rivers -> forest-ish (more danger), else grasslands.
        if ((flags & TileFlags.River) != 0)
            return new ZoneInfo(ZoneId.Forest, Danger: 2, EncounterTableId: "forest_low");

        return new ZoneInfo(ZoneId.Grasslands, Danger: 1, EncounterTableId: "plains_low");
    }
}
