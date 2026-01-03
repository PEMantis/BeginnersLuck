using BeginnersLuck.WorldGen.Data;

namespace BeginnersLuck.WorldGen.Local.Steps;

public sealed class LocalTownStep : ILocalGenStep
{
    public string Name => "LocalTown";

    public void Run(LocalGenContext ctx)
    {
        // Only if this world tile is a town (or request says Town)
        bool isTown = ctx.IsTownTile || ctx.Request.Purpose == LocalMapPurpose.Town;
        if (!isTown) return;

        int n = ctx.Map.Size;
        int cx = n / 2;
        int cy = n / 2;

        // Stamp a “town footprint” (cleared land, gentle flatten)
        int radius = Math.Max(10, n / 8);

        for (int y = cy - radius; y <= cy + radius; y++)
        for (int x = cx - radius; x <= cx + radius; x++)
        {
            if ((uint)x >= (uint)n || (uint)y >= (uint)n) continue;

            int dx = x - cx;
            int dy = y - cy;
            if (dx * dx + dy * dy > radius * radius) continue;

            int idx = ctx.Map.Index(x, y);

            // Flatten-ish: pull toward center elevation
            byte centerE = ctx.Map.Elevation[ctx.Map.Index(cx, cy)];
            byte e = ctx.Map.Elevation[idx];
            int ne = (int)(e + (centerE - e) * 0.35f);
            ctx.Map.Elevation[idx] = (byte)Math.Clamp(ne, 0, 255);

            // Make it buildable-looking
            if (ctx.Map.Terrain[idx] is TileId.DeepWater or TileId.ShallowWater)
                ctx.Map.Terrain[idx] = TileId.Dirt;
            else
                ctx.Map.Terrain[idx] = TileId.Dirt;

            ctx.Map.Flags[idx] |= TileFlags.Town;
        }

        ctx.Set("TownCenter", new Point2i(cx, cy));
    }
}
