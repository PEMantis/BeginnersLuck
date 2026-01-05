using System;
using BeginnersLuck.WorldGen.Data;

namespace BeginnersLuck.WorldGen.Local.Steps;

public sealed class LocalRuinsStep : ILocalGenStep
{
    public string Name => "LocalRuins";

    public void Run(LocalGenContext ctx)
    {
        if (ctx.Request.Purpose != LocalMapPurpose.Ruins)
            return;

        var map = ctx.Map;
        var rng = new Random(ctx.Request.Seed ^ 0x5F3759DF);

        int size = map.Size;
        int n = size * size;

        // Clear any previous ruins flags (just in case)
        for (int i = 0; i < n; i++)
            map.Flags[i] &= ~TileFlags.Ruins;

        // Place clusters of ruin pillars
        int margin = 6;
        int clusters = Math.Clamp(size / 10, 6, 16);

        for (int c = 0; c < clusters; c++)
        {
            int cx = rng.Next(margin, size - margin);
            int cy = rng.Next(margin, size - margin);

            int count = rng.Next(6, 16);
            for (int k = 0; k < count; k++)
            {
                int x = Math.Clamp(cx + rng.Next(-6, 7), margin, size - margin - 1);
                int y = Math.Clamp(cy + rng.Next(-6, 7), margin, size - margin - 1);

                int i = map.Index(x, y);

                // Avoid placing ruins on water tiles (basic heuristic)
                var t = map.Terrain[i];
                if (t is TileId.Ocean or TileId.DeepWater or TileId.ShallowWater)
                    continue;

                map.Flags[i] |= TileFlags.Ruins;
            }
        }

        // Optional: ensure a small clear "center" area for navigation
        int mid = size / 2;
        for (int y = mid - 4; y <= mid + 4; y++)
        for (int x = mid - 6; x <= mid + 6; x++)
        {
            if ((uint)x >= (uint)size || (uint)y >= (uint)size) continue;
            int i = map.Index(x, y);
            map.Flags[i] &= ~TileFlags.Ruins;
        }
    }
}
