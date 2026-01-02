using BeginnersLuck.WorldGen.Data;
using BeginnersLuck.WorldGen.Generation.Pipeline;

namespace BeginnersLuck.WorldGen.Steps;

public sealed class ValleyErosionStep : IWorldGenStep
{
    public string Name => "ValleyErosion";

    public void Run(WorldGenContext ctx)
    {
        int w = ctx.Map.Width;
        int h = ctx.Map.Height;
        int cs = ctx.Map.ChunkSize;

        int r = Math.Max(1, ctx.Request.Settings.RiverErosionRadius);
        int strength = Math.Max(1, ctx.Request.Settings.RiverErosionStrength);

        // We do a two-pass: gather river tiles, then apply erosion.
        // This avoids “newly eroded” tiles affecting detection while we’re iterating.
        var river = new List<Point2i>(w * h / 64);

        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            if ((GetFlags(ctx, x, y, cs) & TileFlags.River) != 0)
                river.Add(new Point2i(x, y));
        }

        foreach (var p in river)
        {
            byte baseE = GetElevation(ctx, p.X, p.Y, cs);

            for (int dy = -r; dy <= r; dy++)
            for (int dx = -r; dx <= r; dx++)
            {
                int px = p.X + dx;
                int py = p.Y + dy;
                if ((uint)px >= (uint)w || (uint)py >= (uint)h) continue;

                int dist = Math.Abs(dx) + Math.Abs(dy);
                if (dist > r) continue;

                // Falloff: strongest at center, weaker at edges
                int drop = (int)MathF.Round(strength * (1f - (dist / (float)r)));
                if (drop <= 0) continue;

                byte e = GetElevation(ctx, px, py, cs);
                // Don’t raise anything; only lower if higher than “river bed” area
                if (e > baseE)
                {
                    int ne = e - drop;
                    if (ne < 0) ne = 0;
                    SetElevation(ctx, px, py, cs, (byte)ne);
                }
            }
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

    private static TileFlags GetFlags(WorldGenContext ctx, int x, int y, int cs)
    {
        var c = ctx.Map.GetChunk(x / cs, y / cs);
        return c.Flags[c.Index(x % cs, y % cs)];
    }
}
