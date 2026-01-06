using BeginnersLuck.WorldGen.Data;

namespace BeginnersLuck.Game.World;

public static class WorldPoiResolver
{
    public static PoiType Resolve(TileId terrain, TileFlags flags)
    {
        // “Hard” POIs first
        if ((flags & TileFlags.Town) != 0)  return PoiType.Town;
        if ((flags & TileFlags.Ruins) != 0) return PoiType.Ruins;

        // Road isn’t really a POI, but it’s useful UI feedback
        if ((flags & TileFlags.Road) != 0)  return PoiType.Road;

        return PoiType.None;
    }

    public static string DisplayName(PoiType t) => t switch
    {
        PoiType.Ruins => "RUINS",
        PoiType.Town  => "TOWN",
        PoiType.Road  => "ROAD",
        _ => ""
    };

    public static string Prompt(PoiType t) => t switch
    {
        PoiType.Ruins => "PRESS E / A TO ENTER",
        PoiType.Town  => "PRESS E / A TO ENTER",
        PoiType.Road  => "PRESS E / A TO TRAVEL",
        _ => ""
    };
}
