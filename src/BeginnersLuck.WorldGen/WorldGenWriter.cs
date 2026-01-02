using System.Text.Json;

namespace BeginnersLuck.WorldGen.Serialization;

public static class WorldWriter
{
    public static void WriteJson(string path, WorldMap map)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var dto = new
        {
            map.Width,
            map.Height,
            map.ChunkSize,
            map.Seed,
            map.GeneratorVersion
        };

        File.WriteAllText(path, JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true }));
    }
}
