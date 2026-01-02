using BeginnersLuck.WorldGen.Data;
using BeginnersLuck.WorldGen.Generation.Pipeline;

namespace BeginnersLuck.WorldGen.Steps;

public sealed class RiverStep : IWorldGenStep
{
    public string Name => "Rivers";

    public void Run(WorldGenContext context)
    {
        if (!context.TryGet("RiverSources", out List<Point2i> sources))
            return;

        int w = context.Map.Width;
        int h = context.Map.Height;
        int cs = context.Map.ChunkSize;

        int seaLevel = context.Get<int>("SeaLevel");
        var s = context.Request.Settings;

        foreach (var src in sources)
            CarveRiver(context, src, w, h, cs, seaLevel, s);
    }

    private static void CarveRiver(
        WorldGenContext ctx,
        Point2i src,
        int w,
        int h,
        int cs,
        int seaLevel,
        WorldGenSettings s)
    {
        var visited = new HashSet<int>(2048);

        int x = src.X;
        int y = src.Y;

        for (int step = 0; step < s.RiverMaxLength; step++)
        {
            int id = y * w + x;
            if (!visited.Add(id))
                break;

            byte e = GetElevation(ctx, x, y, cs);
            if (e <= seaLevel)
                break;

            // Paint river
            PaintRiver(ctx, x, y, cs, RiverWidth(step, s));

            // Choose next step
            if (!TryChooseNext(ctx, x, y, w, h, cs, seaLevel, out int nx, out int ny))
            {
                // FORCE a trench: lower one neighbor slightly
                if (!ForceCarve(ctx, x, y, w, h, cs))
                    break;

                continue;
            }

            x = nx;
            y = ny;
        }
    }

    private static bool TryChooseNext(
        WorldGenContext ctx,
        int x,
        int y,
        int w,
        int h,
        int cs,
        int seaLevel,
        out int nx,
        out int ny)
    {
        Span<(int x, int y)> n = stackalloc (int, int)[]
        {
            (x-1,y),(x+1,y),(x,y-1),(x,y+1),
            (x-1,y-1),(x+1,y-1),(x-1,y+1),(x+1,y+1)
        };

        byte curE = GetElevation(ctx, x, y, cs);

        float best = float.NegativeInfinity;
        nx = x; ny = y;

        for (int i = 0; i < n.Length; i++)
        {
            int tx = n[i].x, ty = n[i].y;
            if ((uint)tx >= (uint)w || (uint)ty >= (uint)h) continue;

            byte te = GetElevation(ctx, tx, ty, cs);
            float score = (curE - te) * 10f;

            if (te <= seaLevel)
                score += 1000f;

            if (score > best)
            {
                best = score;
                nx = tx;
                ny = ty;
            }
        }

        return nx != x || ny != y;
    }

    private static bool ForceCarve(
        WorldGenContext ctx,
        int x,
        int y,
        int w,
        int h,
        int cs)
    {
        Span<(int x, int y)> n = stackalloc (int, int)[]
        {
            (x-1,y),(x+1,y),(x,y-1),(x,y+1)
        };

        byte curE = GetElevation(ctx, x, y, cs);

        for (int i = 0; i < n.Length; i++)
        {
            int tx = n[i].x, ty = n[i].y;
            if ((uint)tx >= (uint)w || (uint)ty >= (uint)h) continue;

            byte te = GetElevation(ctx, tx, ty, cs);
            if (te >= curE)
            {
                SetElevation(ctx, tx, ty, cs, (byte)(curE - 1));
                return true;
            }
        }

        return false;
    }

    private static int RiverWidth(int step, WorldGenSettings s)
    {
        float t = Math.Clamp(step / (float)s.RiverMaxLength, 0f, 1f);
        float v = (s.RiverWidthStart * (1f - t)) + (s.RiverWidthEnd * t);
        return Math.Max(1, (int)MathF.Round(v));
    }

    private static void PaintRiver(WorldGenContext ctx, int x, int y, int cs, int r)
    {
        for (int dy = -r; dy <= r; dy++)
        for (int dx = -r; dx <= r; dx++)
        {
            if (Math.Abs(dx) + Math.Abs(dy) > r) continue;
            int px = x + dx, py = y + dy;
            if ((uint)px >= (uint)ctx.Map.Width || (uint)py >= (uint)ctx.Map.Height) continue;

            OrFlag(ctx, px, py, cs, TileFlags.River);
            SetTerrain(ctx, px, py, cs, TileId.ShallowWater);
        }
    }

    private static byte GetElevation(WorldGenContext ctx, int x, int y, int cs)
    {
        var c = ctx.Map.GetChunk(x / cs, y / cs);
        return c.Elevation[c.Index(x % cs, y % cs)];
    }

    private static void SetElevation(WorldGenContext ctx, int x, int y, int cs, byte v)
    {
        var c = ctx.Map.GetChunk(x / cs, y / cs);
        c.Elevation[c.Index(x % cs, y % cs)] = v;
    }

    private static void SetTerrain(WorldGenContext ctx, int x, int y, int cs, TileId id)
    {
        var c = ctx.Map.GetChunk(x / cs, y / cs);
        c.Terrain[c.Index(x % cs, y % cs)] = id;
    }

    private static void OrFlag(WorldGenContext ctx, int x, int y, int cs, TileFlags f)
    {
        var c = ctx.Map.GetChunk(x / cs, y / cs);
        c.Flags[c.Index(x % cs, y % cs)] |= f;
    }
}
