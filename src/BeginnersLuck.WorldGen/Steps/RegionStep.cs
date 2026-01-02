using BeginnersLuck.WorldGen.Data;
using BeginnersLuck.WorldGen.Generation.Pipeline;

namespace BeginnersLuck.WorldGen.Steps;

public sealed class RegionStep : IWorldGenStep
{
    public string Name => "Regions";

    public void Run(WorldGenContext ctx)
    {
        int w = ctx.Map.Width;
        int h = ctx.Map.Height;
        int cs = ctx.Map.ChunkSize;

        int seaLevel = ctx.Get<int>("SeaLevel");

        // Reset
        foreach (var (cx, cy) in ctx.Map.AllChunkCoords())
        {
            var chunk = ctx.Map.GetChunk(cx, cy);
            Array.Clear(chunk.Region);
        }

        ushort nextId = 1;
        var q = new Queue<int>(4096);

        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            if (IsWater(ctx, x, y, cs, seaLevel)) continue;
            if (GetRegion(ctx, x, y, cs) != 0) continue;

            // Flood fill a new region
            ushort id = nextId++;
            SetRegion(ctx, x, y, cs, id);
            q.Enqueue(y * w + x);

            while (q.Count > 0)
            {
                int cur = q.Dequeue();
                int cxp = cur % w;
                int cyp = cur / w;

                TryVisit(cxp - 1, cyp, id);
                TryVisit(cxp + 1, cyp, id);
                TryVisit(cxp, cyp - 1, id);
                TryVisit(cxp, cyp + 1, id);
            }
        }

        ctx.Set("RegionCount", (int)(nextId - 1));

        void TryVisit(int nx, int ny, ushort id)
        {
            if ((uint)nx >= (uint)w || (uint)ny >= (uint)h) return;
            if (IsWater(ctx, nx, ny, cs, seaLevel)) return;
            if (GetRegion(ctx, nx, ny, cs) != 0) return;

            SetRegion(ctx, nx, ny, cs, id);
            q.Enqueue(ny * w + nx);
        }
    }

    private static bool IsWater(WorldGenContext ctx, int x, int y, int cs, int seaLevel)
    {
        var c = ctx.Map.GetChunk(x / cs, y / cs);
        int i = c.Index(x % cs, y % cs);
        return c.Elevation[i] <= seaLevel || c.Terrain[i] is TileId.DeepWater or TileId.ShallowWater;
    }

    private static ushort GetRegion(WorldGenContext ctx, int x, int y, int cs)
    {
        var c = ctx.Map.GetChunk(x / cs, y / cs);
        return c.Region[c.Index(x % cs, y % cs)];
    }

    private static void SetRegion(WorldGenContext ctx, int x, int y, int cs, ushort id)
    {
        var c = ctx.Map.GetChunk(x / cs, y / cs);
        c.Region[c.Index(x % cs, y % cs)] = id;
    }
}
