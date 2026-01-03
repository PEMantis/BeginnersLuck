using BeginnersLuck.WorldGen.Data;

namespace BeginnersLuck.WorldGen.Local;

public sealed class LocalGenContext
{
    public WorldMap World { get; }
    public LocalMapRequest Request { get; }
    public LocalMap Map { get; }
    public int SeaLevel { get; set; }

    // World tile properties at (WorldX, WorldY)
    public BiomeId Biome { get; set; }
    public bool IsTownTile { get; set; }
    public ushort Region { get; set; }
    public ushort SubRegion { get; set; }

    // Connections detected on the world map around this tile
    public EdgePortals Portals { get; set; } = new();

    private readonly Dictionary<string, object> _bag = new();

    public LocalGenContext(WorldMap world, LocalMapRequest req, LocalMap map)
    {
        World = world;
        Request = req;
        Map = map;
    }

    public void Set<T>(string key, T value) where T : notnull => _bag[key] = value;
    public bool TryGet<T>(string key, out T value)
    {
        if (_bag.TryGetValue(key, out var obj) && obj is T t) { value = t; return true; }
        value = default!;
        return false;
    }

    public int SeedFor(string key)
    {
        unchecked
        {
            int s = Request.Seed;
            s ^= key.GetHashCode();
            s ^= (Request.WorldX * 73856093);
            s ^= (Request.WorldY * 19349663);
            s ^= (s << 13);
            s ^= (s >> 17);
            s ^= (s << 5);
            return s;
        }
    }

    public int SeedFor(string key, int a, int b)
    {
        unchecked
        {
            int s = SeedFor(key);
            s ^= a * (int)83492791;
            s ^= b * (int)2654435761; // overflows int, that's fine in unchecked
            s ^= (s << 13);
            s ^= (s >> 17);
            s ^= (s << 5);
            return s;
        }
    }
}

public enum Edge : byte { North = 0, East = 1, South = 2, West = 3 }

public sealed class EdgePortals
{
    // If a neighbor tile has a river/road and this tile also has it,
    // we create “portal points” along edges to connect across tiles.
    public bool RiverN, RiverE, RiverS, RiverW;
    public bool RoadN, RoadE, RoadS, RoadW;

    // Portal coordinates along each edge (0..Size-1)
    public int RiverNPos, RiverEPos, RiverSPos, RiverWPos;
    public int RoadNPos, RoadEPos, RoadSPos, RoadWPos;
}
