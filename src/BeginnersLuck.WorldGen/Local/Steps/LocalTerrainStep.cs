using BeginnersLuck.WorldGen.Data;

namespace BeginnersLuck.WorldGen.Local.Steps;

public sealed class LocalTerrainStep : ILocalGenStep
{
    public string Name => "LocalTerrain";

    public void Run(LocalGenContext ctx)
    {
        int n = ctx.Map.Size;
        int sea = ctx.SeaLevel;

        // Robust verification: scan the ELEVATION buffer directly
        // (Do NOT scan Terrain/Flags lengths because those could be empty/mismatched during debugging.)
        byte minE = 255, maxE = 0;
        var elev = ctx.Map.Elevation;

        for (int i = 0; i < elev.Length; i++)
        {
            byte v = elev[i];
            if (v < minE) minE = v;
            if (v > maxE) maxE = v;
        }

        Console.WriteLine($"[LocalTerrain] sea={sea} biome={ctx.Biome} elevMin={minE} elevMax={maxE} elevLen={elev.Length}");

        // If elevation is truly zeroed, bail.
        if (maxE == 0 && minE == 0)
            throw new InvalidOperationException("[LocalTerrain] Elevation is all zero. LocalFieldsStep did not populate elevation.");

        // Safe deep-water threshold
        int deepThreshold = Math.Max(0, sea - 8);

        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            int idx = ctx.Map.Index(x, y);
            byte e = ctx.Map.Elevation[idx];

            if (e <= deepThreshold)
            {
                ctx.Map.Terrain[idx] = TileId.DeepWater;
                continue;
            }
            if (e <= sea)
            {
                ctx.Map.Terrain[idx] = TileId.ShallowWater;
                continue;
            }

            // land: basic assignment with biome flavor
            ctx.Map.Terrain[idx] = ctx.Biome switch
            {
                BiomeId.Desert => TileId.Sand,
                BiomeId.Snow or BiomeId.Tundra => (e > sea + 75 ? TileId.Rock : TileId.Snow),
                BiomeId.Mountains => (e > sea + 55 ? TileId.Rock : TileId.Dirt),
                BiomeId.Swamp => TileId.Swamp,
                _ => (e > sea + 80 ? TileId.Rock : TileId.Grass),
            };
        }
    }
}
