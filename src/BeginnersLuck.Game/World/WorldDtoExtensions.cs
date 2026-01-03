using System;

namespace BeginnersLuck.Game.World;

public static class WorldDtoExtensions
{
    public static void Validate(this WorldDto w)
    {
        if (w.Width <= 0 || w.Height <= 0) throw new InvalidOperationException("world.json invalid dimensions.");
        if (w.ChunkSize <= 0) throw new InvalidOperationException("world.json missing ChunkSize.");
        if (w.Chunks == null || w.Chunks.Length == 0) throw new InvalidOperationException("world.json has no Chunks.");
    }

    public static int[] BuildFullTerrain(this WorldDto w)
    {
        w.Validate();

        int[] terrain = new int[w.Width * w.Height];
        Array.Fill(terrain, 0);

        int cs = w.ChunkSize;
        int expectedChunkLen = cs * cs;

        foreach (var c in w.Chunks)
        {
            if (c.Terrain == null || c.Terrain.Length != expectedChunkLen)
                throw new InvalidOperationException($"Chunk ({c.Cx},{c.Cy}) terrain length mismatch: {c.Terrain?.Length ?? 0} vs {expectedChunkLen}");

            int baseX = c.Cx * cs;
            int baseY = c.Cy * cs;

            for (int y = 0; y < cs; y++)
            for (int x = 0; x < cs; x++)
            {
                int src = x + y * cs;
                int wx = baseX + x;
                int wy = baseY + y;

                // guard in case edges ever exist
                if ((uint)wx >= (uint)w.Width || (uint)wy >= (uint)w.Height)
                    continue;

                terrain[wx + wy * w.Width] = c.Terrain[src];
            }
        }

        return terrain;
    }

    public static int[] BuildFullFlags(this WorldDto w)
    {
        w.Validate();

        int[] flags = new int[w.Width * w.Height];
        Array.Fill(flags, 0);

        int cs = w.ChunkSize;
        int expectedChunkLen = cs * cs;

        foreach (var c in w.Chunks)
        {
            if (c.Flags == null || c.Flags.Length != expectedChunkLen)
                continue; // allow missing flags in early versions

            int baseX = c.Cx * cs;
            int baseY = c.Cy * cs;

            for (int y = 0; y < cs; y++)
            for (int x = 0; x < cs; x++)
            {
                int src = x + y * cs;
                int wx = baseX + x;
                int wy = baseY + y;

                if ((uint)wx >= (uint)w.Width || (uint)wy >= (uint)w.Height)
                    continue;

                flags[wx + wy * w.Width] = c.Flags[src];
            }
        }

        return flags;
    }
}
