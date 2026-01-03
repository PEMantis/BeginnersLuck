using BeginnersLuck.WorldGen.Data;

namespace BeginnersLuck.WorldGen.Local.Steps;

public sealed class LocalTerrainStep : ILocalGenStep
{
    public string Name => "LocalTerrain";

    public void Run(LocalGenContext ctx)
    {
        int n = ctx.Map.Size;
        int sea = ctx.SeaLevel;

        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            int idx = ctx.Map.Index(x, y);
            byte e = ctx.Map.Elevation[idx];

            if (e <= sea - 8)
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
