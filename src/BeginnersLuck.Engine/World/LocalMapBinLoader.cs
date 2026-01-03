using System.IO;
using BeginnersLuck.WorldGen.Data;
using BeginnersLuck.WorldGen.Local;

namespace BeginnersLuck.Engine.World;

public static class LocalMapBinLoader
{
    private const uint Magic = 0x504D4C42; // BLMP
    private const ushort SupportedVersionMax = 1;

    public static LocalMapData Load(string path)
    {
        using var fs = File.OpenRead(path);
        using var br = new BinaryReader(fs);

        uint magic = br.ReadUInt32();
        if (magic != Magic)
            throw new InvalidDataException("Not a BLMP local map file.");

        ushort ver = br.ReadUInt16();
        if (ver == 0 || ver > SupportedVersionMax)
            throw new InvalidDataException($"Unsupported BLMP version: {ver}");

        int size = br.ReadUInt16();
        int seed = br.ReadInt32();
        int wx = br.ReadInt32();
        int wy = br.ReadInt32();

        var purpose = (LocalMapPurpose)br.ReadByte();
        var biome = (BiomeId)br.ReadByte();

        int count = size * size;

        var elevation = br.ReadBytes(count);
        var moisture = br.ReadBytes(count);
        var temperature = br.ReadBytes(count);

        var terrainBytes = br.ReadBytes(count);
        var terrain = new TileId[count];
        for (int i = 0; i < count; i++)
            terrain[i] = (TileId)terrainBytes[i];

        var flags = new TileFlags[count];
        for (int i = 0; i < count; i++)
            flags[i] = (TileFlags)br.ReadUInt16();

        var portals = ReadPortals(br);

        (int X, int Y)? townCenter = null;
        byte hasTown = br.ReadByte();
        if (hasTown != 0)
        {
            int tx = br.ReadUInt16();
            int ty = br.ReadUInt16();
            townCenter = (tx, ty);
        }

        return new LocalMapData(
            size, seed, wx, wy,
            purpose, biome,
            elevation, moisture, temperature,
            terrain, flags,
            portals, townCenter);
    }

    private static EdgePortals ReadPortals(BinaryReader br)
    {
        byte riverMask = br.ReadByte();
        byte roadMask = br.ReadByte();

        var p = new EdgePortals
        {
            RiverN = (riverMask & 1) != 0,
            RiverE = (riverMask & 2) != 0,
            RiverS = (riverMask & 4) != 0,
            RiverW = (riverMask & 8) != 0,

            RoadN = (roadMask & 1) != 0,
            RoadE = (roadMask & 2) != 0,
            RoadS = (roadMask & 4) != 0,
            RoadW = (roadMask & 8) != 0,

            RiverNPos = br.ReadUInt16(),
            RiverEPos = br.ReadUInt16(),
            RiverSPos = br.ReadUInt16(),
            RiverWPos = br.ReadUInt16(),

            RoadNPos = br.ReadUInt16(),
            RoadEPos = br.ReadUInt16(),
            RoadSPos = br.ReadUInt16(),
            RoadWPos = br.ReadUInt16(),
        };

        return p;
    }
}
