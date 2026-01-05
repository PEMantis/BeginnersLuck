using System;
using BeginnersLuck.Engine.World;
using BeginnersLuck.WorldGen.Data;
using BeginnersLuck.WorldGen.Local;

namespace BeginnersLuck.Game.World;

/// <summary>
/// Boundary adapter: WorldGen.LocalMap -> Engine.World.LocalMapData
/// Lives in Game because Game orchestrates generation, validation, and persistence.
/// </summary>
public static class LocalMapAdapter
{
    public static LocalMapData ToData(
        LocalMap map,
        LocalMapPurpose purpose,
        BiomeId biome,
        EdgePortals portals,
        (int X, int Y)? townCenter)
    {
        if (map == null) throw new ArgumentNullException(nameof(map));

        int n = map.Size;
        int count = n * n;

        var elevation = (byte[])map.Elevation.Clone();
        var moisture = (byte[])map.Moisture.Clone();
        var temperature = (byte[])map.Temperature.Clone();

        var terrain = new TileId[count];
        var flags = new TileFlags[count];
        
        Array.Copy(map.Terrain, terrain, count);
        Array.Copy(map.Flags, flags, count);

        // IMPORTANT: use positional args (matches your Loader construction)
        return new LocalMapData(
            n,
            map.Seed,
            map.WorldX,
            map.WorldY,
            purpose,
            biome,
            elevation,
            moisture,
            temperature,
            terrain,
            flags,
            portals,
            townCenter
        );
    }
}
