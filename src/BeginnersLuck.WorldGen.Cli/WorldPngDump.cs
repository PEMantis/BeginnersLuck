using BeginnersLuck.WorldGen;
using BeginnersLuck.WorldGen.Data;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace BeginnersLuck.WorldGen.Cli;

public static class WorldPngDump
{
    public static void WriteAll(string outDir, WorldMap map)
    {
        Directory.CreateDirectory(outDir);

        WriteGrayscale(Path.Combine(outDir, "elevation.png"), map, (c) => c.Elevation);
        WriteGrayscale(Path.Combine(outDir, "moisture.png"), map, (c) => c.Moisture);
        WriteGrayscale(Path.Combine(outDir, "temperature.png"), map, (c) => c.Temperature);

        WriteTerrain(Path.Combine(outDir, "terrain.png"), map);
    }

    private static void WriteGrayscale(string path, WorldMap map, Func<Chunk, byte[]> selector)
    {
        using var img = new Image<Rgba32>(map.Width, map.Height);

        int cs = map.ChunkSize;

        for (int y = 0; y < map.Height; y++)
        {
            int cy = y / cs;
            int ly = y % cs;

            for (int x = 0; x < map.Width; x++)
            {
                int cx = x / cs;
                int lx = x % cs;

                var chunk = map.GetChunk(cx, cy);
                var arr = selector(chunk);

                int idx = (ly * cs) + lx;
                byte v = arr[idx];

                img[x, y] = new Rgba32(v, v, v, 255);
            }
        }

        img.Save(path);
    }

    private static void WriteTerrain(string path, WorldMap map)
    {
        using var img = new Image<Rgba32>(map.Width, map.Height);

        int cs = map.ChunkSize;

        for (int y = 0; y < map.Height; y++)
        {
            int cy = y / cs;
            int ly = y % cs;

            for (int x = 0; x < map.Width; x++)
            {
                int cx = x / cs;
                int lx = x % cs;

                var chunk = map.GetChunk(cx, cy);
                int idx = (ly * cs) + lx;

                var tile = chunk.Terrain[idx];
                img[x, y] = TerrainColor(tile);
            }
        }

        img.Save(path);
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
