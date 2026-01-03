using BeginnersLuck.WorldGen.Data;

namespace BeginnersLuck.WorldGen.Local.Export;

public sealed class LocalMapMeta
{
    public ushort Version { get; set; } = LocalMapExport.CurrentVersion;

    public int Size { get; set; }
    public int Seed { get; set; }
    public int WorldX { get; set; }
    public int WorldY { get; set; }

    public string Purpose { get; set; } = "wild";
    public string Biome { get; set; } = "Unknown";

    public EdgePortals Portals { get; set; } = new();

    public bool HasTownCenter { get; set; }
    public int TownCenterX { get; set; }
    public int TownCenterY { get; set; }

    public int WaterTiles { get; set; }
    public int RiverTiles { get; set; }
    public int RoadTiles { get; set; }
    public int TownTiles { get; set; }
}
