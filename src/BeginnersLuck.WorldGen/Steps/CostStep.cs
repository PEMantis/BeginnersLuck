using BeginnersLuck.WorldGen.Data;
using BeginnersLuck.WorldGen.Generation.Pipeline;

namespace BeginnersLuck.WorldGen.Steps;

public sealed class CoastStep : IWorldGenStep
{
    public string Name => "Coast";

    public void Run(WorldGenContext context)
    {
        int w = context.Map.Width;
        int h = context.Map.Height;
        int cs = context.Map.ChunkSize;

        int seaLevel = context.Get<int>("SeaLevel");
        int shallowBand = Math.Max(1, context.Request.Settings.ShallowWaterBand);

        // Distance to nearest land for each tile (BFS).
        // Land tiles start at dist=0; water gets dist>=1.
        var dist = new int[w * h];
        Array.Fill(dist, -1);

        var q = new Queue<int>(w * h / 8);

        // Seed BFS with ALL land tiles (dist=0)
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            byte e = GetElevation(context, x, y, cs);
            bool isLand = e > seaLevel;

            if (isLand)
            {
                int idx = y * w + x;
                dist[idx] = 0;
                q.Enqueue(idx);
            }
        }

        // BFS 4-neighbors
        while (q.Count > 0)
        {
            int cur = q.Dequeue();
            int cx = cur % w;
            int cy = cur / w;
            int cd = dist[cur];

            // Early out: once we're beyond shallow band, we still need to fill distances
            // for correct classification further out, but you can keep this simple for now.

            TryVisit(cx - 1, cy, cd + 1);
            TryVisit(cx + 1, cy, cd + 1);
            TryVisit(cx, cy - 1, cd + 1);
            TryVisit(cx, cy + 1, cd + 1);
        }

        // Classify water as shallow/deep using distance-to-land
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            byte e = GetElevation(context, x, y, cs);
            if (e > seaLevel)
                continue; // land

            int d = dist[y * w + x]; // should be >=1 for water
            SetTerrain(context, x, y, cs, (d >= 0 && d <= shallowBand) ? TileId.ShallowWater : TileId.DeepWater);
        }

        // Flag coasts (land adjacent to water)
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            byte e = GetElevation(context, x, y, cs);
            if (e <= seaLevel) continue; // only land tiles get Coast flag

            if (HasWaterNeighbor(context, x, y, w, h, cs, seaLevel))
                OrFlag(context, x, y, cs, TileFlags.Coast);
        }

        void TryVisit(int nx, int ny, int nd)
        {
            if ((uint)nx >= (uint)w || (uint)ny >= (uint)h) return;

            int nidx = ny * w + nx;
            if (dist[nidx] != -1) return;

            dist[nidx] = nd;
            q.Enqueue(nidx);
        }
    }

    private static bool HasWaterNeighbor(WorldGenContext ctx, int x, int y, int w, int h, int cs, int seaLevel)
    {
        return IsWater(ctx, x - 1, y, w, h, cs, seaLevel)
            || IsWater(ctx, x + 1, y, w, h, cs, seaLevel)
            || IsWater(ctx, x, y - 1, w, h, cs, seaLevel)
            || IsWater(ctx, x, y + 1, w, h, cs, seaLevel);
    }

    private static bool IsWater(WorldGenContext ctx, int x, int y, int w, int h, int cs, int seaLevel)
    {
        if ((uint)x >= (uint)w || (uint)y >= (uint)h) return false;
        return GetElevation(ctx, x, y, cs) <= seaLevel;
    }

    private static byte GetElevation(WorldGenContext ctx, int x, int y, int cs)
    {
        int cx = x / cs; int cy = y / cs;
        int lx = x % cs; int ly = y % cs;
        var chunk = ctx.Map.GetChunk(cx, cy);
        return chunk.Elevation[chunk.Index(lx, ly)];
    }

    private static void SetTerrain(WorldGenContext ctx, int x, int y, int cs, TileId id)
    {
        int cx = x / cs; int cy = y / cs;
        int lx = x % cs; int ly = y % cs;
        var chunk = ctx.Map.GetChunk(cx, cy);
        chunk.Terrain[chunk.Index(lx, ly)] = id;
    }

    private static void OrFlag(WorldGenContext ctx, int x, int y, int cs, TileFlags flag)
    {
        int cx = x / cs; int cy = y / cs;
        int lx = x % cs; int ly = y % cs;
        var chunk = ctx.Map.GetChunk(cx, cy);
        int i = chunk.Index(lx, ly);
        chunk.Flags[i] |= flag;
    }
}
