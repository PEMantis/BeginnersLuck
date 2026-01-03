using System;
using System.IO;
using BeginnersLuck.WorldGen.Data;
using BeginnersLuck.WorldGen.Local;

namespace BeginnersLuck.Engine.World;

public static class LocalMapBinWriter
{
    private const uint Magic = 0x504D4C42; // BLMP
    private const ushort Version = 1;

    public static void Save(string path, LocalMapData map)
    {
        if (map == null) throw new ArgumentNullException(nameof(map));

        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        using var fs = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
        using var bw = new BinaryWriter(fs);

        bw.Write(Magic);
        bw.Write(Version);

        bw.Write((ushort)map.Size);
        bw.Write(map.Seed);
        bw.Write(map.WorldX);
        bw.Write(map.WorldY);

        bw.Write((byte)map.Purpose);
        bw.Write((byte)map.Biome);

        int count = map.Size * map.Size;

        WriteExactBytes(bw, map.Elevation, count, nameof(map.Elevation));
        WriteExactBytes(bw, map.Moisture, count, nameof(map.Moisture));
        WriteExactBytes(bw, map.Temperature, count, nameof(map.Temperature));

        if (map.Terrain == null || map.Terrain.Length != count)
            throw new InvalidDataException($"Terrain length {(map.Terrain?.Length ?? 0)} != {count}");

        var terrainBytes = new byte[count];
        for (int i = 0; i < count; i++)
            terrainBytes[i] = (byte)map.Terrain[i];

        bw.Write(terrainBytes);

        if (map.Flags == null || map.Flags.Length != count)
            throw new InvalidDataException($"Flags length {(map.Flags?.Length ?? 0)} != {count}");

        for (int i = 0; i < count; i++)
            bw.Write((ushort)map.Flags[i]);

        WritePortals(bw, map.Portals);

        if (map.TownCenter.HasValue)
        {
            bw.Write((byte)1);
            bw.Write((ushort)map.TownCenter.Value.X);
            bw.Write((ushort)map.TownCenter.Value.Y);
        }
        else
        {
            bw.Write((byte)0);
        }
    }

    private static void WriteExactBytes(BinaryWriter bw, byte[] data, int count, string name)
    {
        if (data == null) throw new InvalidDataException($"{name} is null");
        if (data.Length != count) throw new InvalidDataException($"{name} length {data.Length} != {count}");
        bw.Write(data);
    }

    private static void WritePortals(BinaryWriter bw, EdgePortals p)
    {
        byte riverMask = 0;
        if (p.RiverN) riverMask |= 1;
        if (p.RiverE) riverMask |= 2;
        if (p.RiverS) riverMask |= 4;
        if (p.RiverW) riverMask |= 8;

        byte roadMask = 0;
        if (p.RoadN) roadMask |= 1;
        if (p.RoadE) roadMask |= 2;
        if (p.RoadS) roadMask |= 4;
        if (p.RoadW) roadMask |= 8;

        bw.Write(riverMask);
        bw.Write(roadMask);

        bw.Write((ushort)p.RiverNPos);
        bw.Write((ushort)p.RiverEPos);
        bw.Write((ushort)p.RiverSPos);
        bw.Write((ushort)p.RiverWPos);

        bw.Write((ushort)p.RoadNPos);
        bw.Write((ushort)p.RoadEPos);
        bw.Write((ushort)p.RoadSPos);
        bw.Write((ushort)p.RoadWPos);
    }
}
