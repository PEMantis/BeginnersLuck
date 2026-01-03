using System.Text.Json;
using BeginnersLuck.WorldGen.Data;

namespace BeginnersLuck.WorldGen.Serialization;

public static class WorldWriter
{
    public static void WriteJson(string path, WorldMap map)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var dto = ToDto(map);

        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(path, json);
    }

    private static WorldDto ToDto(WorldMap map)
    {
        var dto = new WorldDto
        {
            Width = map.Width,
            Height = map.Height,
            ChunkSize = map.ChunkSize,
            Seed = map.Seed,
            GeneratorVersion = map.GeneratorVersion,
        };

        foreach (var (cx, cy) in map.AllChunkCoords())
        {
            var c = map.GetChunk(cx, cy);
            int n = map.ChunkSize * map.ChunkSize;

            var cd = new ChunkDto
            {
                Cx = cx,
                Cy = cy,

                Elevation = c.Elevation.ToArray(),
                Moisture = c.Moisture.ToArray(),
                Temperature = c.Temperature.ToArray(),

                Terrain = new byte[n],
                Flags = new ushort[n],
                Biome = new byte[n],

                Region = c.Region.ToArray(),
                SubRegion = c.SubRegion.ToArray(),
            };

            for (int i = 0; i < n; i++)
            {
                cd.Terrain[i] = (byte)c.Terrain[i];
                cd.Flags[i] = (ushort)c.Flags[i];
                cd.Biome[i] = (byte)c.Biome[i];
            }

            dto.Chunks.Add(cd);
        }

        return dto;
    }
}
