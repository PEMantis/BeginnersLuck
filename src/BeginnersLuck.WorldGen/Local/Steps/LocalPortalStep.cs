using BeginnersLuck.WorldGen.Data;

namespace BeginnersLuck.WorldGen.Local.Steps;

public sealed class LocalPortalStep : ILocalGenStep
{
    public string Name => "LocalPortals";

    public void Run(LocalGenContext ctx)
    {
        int wx = ctx.Request.WorldX;
        int wy = ctx.Request.WorldY;
        int cs = ctx.World.ChunkSize;

        var hereFlags = ReadWorldFlags(ctx, wx, wy, cs);
        bool hereRiver = (hereFlags & TileFlags.River) != 0;
        bool hereRoad  = (hereFlags & TileFlags.Road)  != 0;

        var p = new EdgePortals();

        // True continuity: feature must exist in BOTH tiles to cross boundary.
        p.RiverN = hereRiver && Has(ctx, wx, wy - 1, cs, TileFlags.River);
        p.RiverS = hereRiver && Has(ctx, wx, wy + 1, cs, TileFlags.River);
        p.RiverW = hereRiver && Has(ctx, wx - 1, wy, cs, TileFlags.River);
        p.RiverE = hereRiver && Has(ctx, wx + 1, wy, cs, TileFlags.River);

        p.RoadN = hereRoad && Has(ctx, wx, wy - 1, cs, TileFlags.Road);
        p.RoadS = hereRoad && Has(ctx, wx, wy + 1, cs, TileFlags.Road);
        p.RoadW = hereRoad && Has(ctx, wx - 1, wy, cs, TileFlags.Road);
        p.RoadE = hereRoad && Has(ctx, wx + 1, wy, cs, TileFlags.Road);

        int n = ctx.Map.Size;
        int inset = Math.Max(3, n / 16);

        if (p.RiverN) p.RiverNPos = EdgePos(ctx, Edge.North, wx, wy, n, inset, "River");
        if (p.RiverS) p.RiverSPos = EdgePos(ctx, Edge.South, wx, wy, n, inset, "River");
        if (p.RiverW) p.RiverWPos = EdgePos(ctx, Edge.West,  wx, wy, n, inset, "River");
        if (p.RiverE) p.RiverEPos = EdgePos(ctx, Edge.East,  wx, wy, n, inset, "River");

        if (p.RoadN) p.RoadNPos = EdgePos(ctx, Edge.North, wx, wy, n, inset, "Road");
        if (p.RoadS) p.RoadSPos = EdgePos(ctx, Edge.South, wx, wy, n, inset, "Road");
        if (p.RoadW) p.RoadWPos = EdgePos(ctx, Edge.West,  wx, wy, n, inset, "Road");
        if (p.RoadE) p.RoadEPos = EdgePos(ctx, Edge.East,  wx, wy, n, inset, "Road");

        ctx.Portals = p;
    }

    private static bool Has(LocalGenContext ctx, int wx, int wy, int cs, TileFlags f)
        => (ReadWorldFlags(ctx, wx, wy, cs) & f) != 0;

    private static TileFlags ReadWorldFlags(LocalGenContext ctx, int wx, int wy, int cs)
    {
        if ((uint)wx >= (uint)ctx.World.Width || (uint)wy >= (uint)ctx.World.Height)
            return TileFlags.None;

        var c = ctx.World.GetChunk(wx / cs, wy / cs);
        int idx = c.Index(wx % cs, wy % cs);
        return c.Flags[idx];
    }

    private static int EdgePos(LocalGenContext ctx, Edge edge, int wx, int wy, int n, int inset, string channel)
    {
        // Symmetric edge key: same seed for both sides of an edge.
        int ax = wx, ay = wy, bx = wx, by = wy;
        switch (edge)
        {
            case Edge.North: bx = wx; by = wy - 1; break;
            case Edge.South: bx = wx; by = wy + 1; break;
            case Edge.West:  bx = wx - 1; by = wy; break;
            case Edge.East:  bx = wx + 1; by = wy; break;
        }

        int mx = Math.Min(ax, bx);
        int my = Math.Min(ay, by);
        int Mx = Math.Max(ax, bx);
        int My = Math.Max(ay, by);

        int seed = ctx.SeedFor($"Portal_{channel}", mx * 1024 + my, Mx * 1024 + My);
        var r = new Random(seed);

        return r.Next(inset, n - inset);
    }
}
