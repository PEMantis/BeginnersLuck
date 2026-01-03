using BeginnersLuck.WorldGen.Data;
using BeginnersLuck.WorldGen.Local;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BeginnersLuck.WorldGen.Cli;

public static class LocalPngDump
{
    public static void WriteAll(string outDir, LocalMap map)
    {
        Directory.CreateDirectory(outDir);

        WriteGrayscale(Path.Combine(outDir, "local_elevation.png"), map, map.Elevation);
        WriteTerrain(Path.Combine(outDir, "local_terrain.png"), map);
        WriteRoads(Path.Combine(outDir, "local_roads.png"), map);
    }

    private static void WriteGrayscale(string path, LocalMap map, byte[] src)
    {
        using var img = new Image<Rgba32>(map.Size, map.Size);
        for (int y = 0; y < map.Size; y++)
        for (int x = 0; x < map.Size; x++)
        {
            byte v = src[map.Index(x, y)];
            img[x, y] = new Rgba32(v, v, v, 255);
        }
        img.Save(path);
    }

    private static void WriteTerrain(string path, LocalMap map)
    {
        using var img = new Image<Rgba32>(map.Size, map.Size);

        for (int y = 0; y < map.Size; y++)
        for (int x = 0; x < map.Size; x++)
        {
            int idx = map.Index(x, y);
            img[x, y] = TerrainColor(map.Terrain[idx]);

            if ((map.Flags[idx] & TileFlags.River) != 0)
                img[x, y] = new Rgba32(70, 140, 220, 255);

            if ((map.Flags[idx] & TileFlags.Town) != 0)
                img[x, y] = new Rgba32(255, 215, 0, 255);
        }

        img.Save(path);
    }

    private static void WriteRoads(string path, LocalMap map)
    {
        using var img = new Image<Rgba32>(map.Size, map.Size);

        for (int y = 0; y < map.Size; y++)
        for (int x = 0; x < map.Size; x++)
        {
            int idx = map.Index(x, y);
            img[x, y] = TerrainColor(map.Terrain[idx]);
        }

        var road = new Rgba32(235, 140, 40, 255);

        for (int y = 0; y < map.Size; y++)
        for (int x = 0; x < map.Size; x++)
        {
            int idx = map.Index(x, y);

            if ((map.Flags[idx] & TileFlags.River) != 0)
                img[x, y] = new Rgba32(70, 140, 220, 255);

            if ((map.Flags[idx] & TileFlags.Road) != 0)
                img[x, y] = road;

            if ((map.Flags[idx] & TileFlags.Town) != 0)
                StampDot(img, x, y, 3, new Rgba32(255, 215, 0, 255));
        }

        img.Save(path);
    }

    private static void StampDot(Image<Rgba32> img, int x, int y, int r, Rgba32 color)
    {
        int w = img.Width;
        int h = img.Height;

        for (int dy = -r; dy <= r; dy++)
        for (int dx = -r; dx <= r; dx++)
        {
            if (dx * dx + dy * dy > r * r) continue;
            int px = x + dx;
            int py = y + dy;
            if ((uint)px >= (uint)w || (uint)py >= (uint)h) continue;
            img[px, py] = color;
        }
    }

    private static Rgba32 TerrainColor(TileId t) => t switch
    {
        TileId.DeepWater    => new Rgba32(10, 20, 60, 255),
        TileId.ShallowWater => new Rgba32(30, 80, 140, 255),
        TileId.Sand         => new Rgba32(194, 178, 128, 255),
        TileId.Grass        => new Rgba32(40, 120, 40, 255),
        TileId.Dirt         => new Rgba32(110, 85, 55, 255),
        TileId.Rock         => new Rgba32(110, 110, 115, 255),
        TileId.Snow         => new Rgba32(235, 235, 245, 255),
        TileId.Swamp        => new Rgba32(40, 70, 40, 255),
        _                   => new Rgba32(0, 0, 0, 255),
    };
}
