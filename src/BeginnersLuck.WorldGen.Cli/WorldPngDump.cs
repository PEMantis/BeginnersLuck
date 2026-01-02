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

        WriteGrayscale(Path.Combine(outDir, "elevation.png"), map, c => c.Elevation);
        WriteGrayscale(Path.Combine(outDir, "moisture.png"), map, c => c.Moisture);
        WriteGrayscale(Path.Combine(outDir, "temperature.png"), map, c => c.Temperature);

        WriteTerrain(Path.Combine(outDir, "terrain.png"), map);
        WriteBiome(Path.Combine(outDir, "biome.png"), map);
        WriteRegions(Path.Combine(outDir, "regions.png"), map);
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

                // Overlays
                if ((chunk.Flags[idx] & TileFlags.River) != 0)
                    img[x, y] = new Rgba32(70, 140, 220, 255);

                if ((chunk.Flags[idx] & TileFlags.RiverSource) != 0)
                    img[x, y] = new Rgba32(255, 60, 60, 255);

                if ((chunk.Flags[idx] & TileFlags.Town) != 0)
                    img[x, y] = new Rgba32(255, 215, 0, 255);
            }
        }

        img.Save(path);
    }

    private static void WriteBiome(string path, WorldMap map)
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

                var b = chunk.Biome[idx];
                img[x, y] = BiomeColor(b);

                // Optional overlay hints
                if ((chunk.Flags[idx] & TileFlags.River) != 0)
                    img[x, y] = new Rgba32(70, 140, 220, 255);

                if ((chunk.Flags[idx] & TileFlags.Town) != 0)
                    img[x, y] = new Rgba32(255, 215, 0, 255);
            }
        }

        img.Save(path);
    }

    private static void WriteRegions(string path, WorldMap map)
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

                ushort region = chunk.Region[idx];

                // Region 0 = water/unassigned -> draw dark
                img[x, y] = region == 0
                    ? new Rgba32(10, 10, 20, 255)
                    : RegionColor(region);
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

    private static Rgba32 BiomeColor(BiomeId b) => b switch
    {
        BiomeId.Ocean     => new Rgba32(10, 20, 60, 255),
        BiomeId.Coast     => new Rgba32(30, 80, 140, 255),

        BiomeId.Plains    => new Rgba32(70, 150, 70, 255),
        BiomeId.Forest    => new Rgba32(25, 95, 35, 255),
        BiomeId.Desert    => new Rgba32(210, 185, 120, 255),
        BiomeId.Swamp     => new Rgba32(40, 80, 55, 255),
        BiomeId.Tundra    => new Rgba32(155, 175, 175, 255),
        BiomeId.Snow      => new Rgba32(235, 235, 245, 255),

        BiomeId.Hills     => new Rgba32(120, 140, 90, 255),
        BiomeId.Mountains => new Rgba32(145, 145, 150, 255),

        _ => new Rgba32(0, 0, 0, 255),
    };

    private static Rgba32 RegionColor(ushort id)
    {
        // Deterministic pastel-ish color based on region id
        uint x = id;
        x ^= x << 13;
        x ^= x >> 17;
        x ^= x << 5;

        byte r = (byte)(80 + (x & 0x7F));       // 80..207
        byte g = (byte)(80 + ((x >> 8) & 0x7F));
        byte b = (byte)(80 + ((x >> 16) & 0x7F));
        return new Rgba32(r, g, b, 255);
    }
}
