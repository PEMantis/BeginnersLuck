using System.Text.Json;
using BeginnersLuck.WorldGen.Data;

namespace BeginnersLuck.WorldGen.Export;

public static class WorldExport
{
    public static void WriteWorldJson(string path, WorldMap world)
    {
        var dto = new WorldExportDto
        {
            Width = world.Width,
            Height = world.Height,
            ChunkSize = world.ChunkSize,
            Seed = world.Seed,
            GeneratorVersion = world.GeneratorVersion
        };

        foreach (var (cx, cy) in world.AllChunkCoords())
        {
            var c = world.GetChunk(cx, cy);
            dto.Chunks.Add(new ChunkExportDto
            {
                Cx = cx,
                Cy = cy,
                Terrain = ToByteArray(c.Terrain),
                Biome = ToByteArray(c.Biome),
                Region = c.Region.ToArray(),
                SubRegion = c.SubRegion.ToArray(),
                Flags = ToUShortArray(c.Flags),
            });

        }

        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    private static byte[] ToByteArray(TileId[] src)
    {
        var b = new byte[src.Length];
        for (int i = 0; i < b.Length; i++) b[i] = (byte)src[i];
        return b;
    }

    private static byte[] ToByteArray(BiomeId[] src)
    {
        var b = new byte[src.Length];
        for (int i = 0; i < b.Length; i++) b[i] = (byte)src[i];
        return b;
    }

    private static byte[] ToByteArray(byte[] src) => src.ToArray();

    private static ushort[] ToUShortArray(TileFlags[] src)
    {
        var a = new ushort[src.Length];
        for (int i = 0; i < a.Length; i++) a[i] = (ushort)src[i];
        return a;
    }
}
