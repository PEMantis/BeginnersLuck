using System;
using System.Collections.Generic;

namespace BeginnersLuck.Game.World;

public sealed class WorldDto
{
    public int Width { get; set; }
    public int Height { get; set; }
    public int ChunkSize { get; set; }
    public int Seed { get; set; }
    public int GeneratorVersion { get; set; }

    public List<ChunkDto> Chunks { get; set; } = new();

    public void Validate()
    {
        if (Width <= 0 || Height <= 0 || ChunkSize <= 0)
            throw new InvalidOperationException("world.json invalid header.");

        if (Chunks == null || Chunks.Count == 0)
            throw new InvalidOperationException("world.json has no chunks.");
    }

    public byte[] BuildFullTerrain()
    {
        Validate();

        int w = Width;
        int h = Height;
        int cs = ChunkSize;

        var terrain = new byte[w * h];

        foreach (var ch in Chunks)
        {
            int baseX = ch.Cx * cs;
            int baseY = ch.Cy * cs;

            if (ch.Terrain.Length != cs * cs)
                throw new InvalidOperationException($"Chunk({ch.Cx},{ch.Cy}) Terrain length mismatch.");

            for (int ly = 0; ly < cs; ly++)
            for (int lx = 0; lx < cs; lx++)
            {
                int local = lx + ly * cs;
                int wx = baseX + lx;
                int wy = baseY + ly;

                int i = wx + wy * w;
                terrain[i] = ch.Terrain[local];
            }
        }

        return terrain;
    }

    public ushort[] BuildFullFlags()
    {
        Validate();

        int w = Width;
        int h = Height;
        int cs = ChunkSize;

        var flags = new ushort[w * h];

        foreach (var ch in Chunks)
        {
            int baseX = ch.Cx * cs;
            int baseY = ch.Cy * cs;

            if (ch.Flags.Length != cs * cs)
                throw new InvalidOperationException($"Chunk({ch.Cx},{ch.Cy}) Flags length mismatch.");

            for (int ly = 0; ly < cs; ly++)
            for (int lx = 0; lx < cs; lx++)
            {
                int local = lx + ly * cs;
                int wx = baseX + lx;
                int wy = baseY + ly;

                int i = wx + wy * w;
                flags[i] = ch.Flags[local];
            }
        }

        return flags;
    }
}

public sealed class ChunkDto
{
    public int Cx { get; set; }
    public int Cy { get; set; }

    public byte[] Elevation { get; set; } = Array.Empty<byte>();
    public byte[] Moisture { get; set; } = Array.Empty<byte>();
    public byte[] Temperature { get; set; } = Array.Empty<byte>();

    // ✅ IMPORTANT: match the writer
    public byte[] Terrain { get; set; } = Array.Empty<byte>();
    public ushort[] Flags { get; set; } = Array.Empty<ushort>();
    public byte[] Biome { get; set; } = Array.Empty<byte>();

    public ushort[] Region { get; set; } = Array.Empty<ushort>();
    public ushort[] SubRegion { get; set; } = Array.Empty<ushort>();
}
