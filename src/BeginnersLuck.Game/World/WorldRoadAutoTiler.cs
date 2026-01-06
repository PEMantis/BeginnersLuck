using BeginnersLuck.WorldGen.Data;

namespace BeginnersLuck.Game.World;

public static class WorldRoadAutoTiler
{
    public static string Resolve(bool n, bool e, bool s, bool w)
    {
        int count = (n ? 1 : 0) + (e ? 1 : 0) + (s ? 1 : 0) + (w ? 1 : 0);

        return count switch
        {
            0 => "world.road.dot",

            1 => n ? "world.road.end_n"
               : e ? "world.road.end_e"
               : s ? "world.road.end_s"
               :     "world.road.end_w",

            2 => (n && s) ? "world.road.straight_v"
               : (e && w) ? "world.road.straight_h"
               : (n && e) ? "world.road.corner_ne"
               : (n && w) ? "world.road.corner_nw"
               : (s && e) ? "world.road.corner_se"
               :           "world.road.corner_sw",

            3 => !n ? "world.road.t_n"
               : !e ? "world.road.t_e"
               : !s ? "world.road.t_s"
               :      "world.road.t_w",

            _ => "world.road.cross"
        };
    }

    public static bool HasRoad(TileFlags f) => (f & TileFlags.Road) != 0;
}
