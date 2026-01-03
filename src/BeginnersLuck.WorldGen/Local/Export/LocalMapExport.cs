using System.Text.Json;
using BeginnersLuck.WorldGen.Data;

namespace BeginnersLuck.WorldGen.Local.Export;

public static class LocalMapExport
{
    public const ushort CurrentVersion = 1;

    private const uint Magic = 0x504D4C42; // 'B''L''M''P' little-endian
    private const string MetaFileName = "local.meta.json";
    private const string BinFileName = "local.mapbin";

    public static void Write(string outDir, LocalGenContext ctx)
    {
        Directory.CreateDirectory(outDir);

        // 1) Write binary payload
        WriteBinary(Path.Combine(outDir, BinFileName), ctx);

        // 2) Write metadata JSON
        var meta = BuildMeta(ctx);
        var json = JsonSerializer.Serialize(meta, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(Path.Combine(outDir, MetaFileName), json);
    }

    public static LocalMapMeta BuildMeta(LocalGenContext ctx)
    {
        var m = new LocalMapMeta
        {
            Version = CurrentVersion,
            Size = ctx.Map.Size,
            Seed = ctx.Map.Seed,
            WorldX = ctx.Map.WorldX,
            WorldY = ctx.Map.WorldY,
            Purpose = ctx.Request.Purpose == LocalMapPurpose.Town ? "town" : "wild",
            Biome = ctx.Biome.ToString(),
            Portals = ctx.Portals
        };

        if (ctx.TryGet("TownCenter", out Point2i c))
        {
            m.HasTownCenter = true;
            m.TownCenterX = c.X;
            m.TownCenterY = c.Y;
        }

        // Counts
        int n = ctx.Map.Size * ctx.Map.Size;
        for (int i = 0; i < n; i++)
        {
            var t = ctx.Map.Terrain[i];
            var f = ctx.Map.Flags[i];

            if (t is TileId.DeepWater or TileId.ShallowWater) m.WaterTiles++;
            if ((f & TileFlags.River) != 0) m.RiverTiles++;
            if ((f & TileFlags.Road) != 0) m.RoadTiles++;
            if ((f & TileFlags.Town) != 0) m.TownTiles++;
        }

        return m;
    }

    public static void WriteBinary(string path, LocalGenContext ctx)
    {
        var map = ctx.Map;
        int size = map.Size;
        int count = size * size;

        using var fs = File.Create(path);
        using var bw = new BinaryWriter(fs);

        // Header
        bw.Write(Magic);
        bw.Write(CurrentVersion);
        bw.Write((ushort)size);

        bw.Write(map.Seed);
        bw.Write(map.WorldX);
        bw.Write(map.WorldY);

        // Optional: biome + purpose as bytes (so runtime can branch without reading meta)
        bw.Write((byte)ctx.Request.Purpose);
        bw.Write((byte)ctx.Biome);

        // Payload
        bw.Write(map.Elevation, 0, count);
        bw.Write(map.Moisture, 0, count);
        bw.Write(map.Temperature, 0, count);

        // Terrain as bytes
        var terrainBytes = new byte[count];
        for (int i = 0; i < count; i++)
            terrainBytes[i] = (byte)map.Terrain[i];
        bw.Write(terrainBytes, 0, count);

        // Flags as ushort (2 bytes each)
        for (int i = 0; i < count; i++)
            bw.Write((ushort)map.Flags[i]);

        // Portals (to avoid needing meta for continuity)
        WritePortals(bw, ctx.Portals);

        // Town center (optional)
        if (ctx.TryGet("TownCenter", out Point2i tc))
        {
            bw.Write((byte)1);
            bw.Write((ushort)tc.X);
            bw.Write((ushort)tc.Y);
        }
        else
        {
            bw.Write((byte)0);
        }
    }

    public static LocalMapReadResult ReadBinary(string path)
    {
        using var fs = File.OpenRead(path);
        using var br = new BinaryReader(fs);

        uint magic = br.ReadUInt32();
        if (magic != Magic)
            throw new InvalidDataException("Not a BLMP local map file.");

        ushort ver = br.ReadUInt16();
        if (ver == 0 || ver > CurrentVersion)
            throw new InvalidDataException($"Unsupported BLMP version: {ver}");

        int size = br.ReadUInt16();
        int seed = br.ReadInt32();
        int wx = br.ReadInt32();
        int wy = br.ReadInt32();

        var purpose = (LocalMapPurpose)br.ReadByte();
        var biome = (BiomeId)br.ReadByte();

        int count = size * size;

        var map = new LocalMap(size, seed, wx, wy);

        br.Read(map.Elevation, 0, count);
        br.Read(map.Moisture, 0, count);
        br.Read(map.Temperature, 0, count);

        var terrainBytes = br.ReadBytes(count);
        for (int i = 0; i < count; i++)
            map.Terrain[i] = (TileId)terrainBytes[i];

        for (int i = 0; i < count; i++)
            map.Flags[i] = (TileFlags)br.ReadUInt16();

        var portals = ReadPortals(br);

        Point2i? townCenter = null;
        byte hasTown = br.ReadByte();
        if (hasTown != 0)
        {
            int tx = br.ReadUInt16();
            int ty = br.ReadUInt16();
            townCenter = new Point2i(tx, ty);
        }

        return new LocalMapReadResult(map, ver, purpose, biome, portals, townCenter);
    }

    private static void WritePortals(BinaryWriter bw, EdgePortals p)
    {
        // Pack booleans into bytes
        bw.Write((byte)(
            (p.RiverN ? 1 : 0) |
            (p.RiverE ? 2 : 0) |
            (p.RiverS ? 4 : 0) |
            (p.RiverW ? 8 : 0)));

        bw.Write((byte)(
            (p.RoadN ? 1 : 0) |
            (p.RoadE ? 2 : 0) |
            (p.RoadS ? 4 : 0) |
            (p.RoadW ? 8 : 0)));

        bw.Write((ushort)p.RiverNPos);
        bw.Write((ushort)p.RiverEPos);
        bw.Write((ushort)p.RiverSPos);
        bw.Write((ushort)p.RiverWPos);

        bw.Write((ushort)p.RoadNPos);
        bw.Write((ushort)p.RoadEPos);
        bw.Write((ushort)p.RoadSPos);
        bw.Write((ushort)p.RoadWPos);
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

public sealed record LocalMapReadResult(
    LocalMap Map,
    ushort Version,
    LocalMapPurpose Purpose,
    BiomeId Biome,
    EdgePortals Portals,
    Point2i? TownCenter
);
