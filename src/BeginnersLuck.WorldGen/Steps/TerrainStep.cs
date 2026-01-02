using BeginnersLuck.WorldGen.Data;
using BeginnersLuck.WorldGen.Generation.Pipeline;

namespace BeginnersLuck.WorldGen.Steps;

public sealed class TerrainStep : IWorldGenStep
{
    public string Name => "Terrain";

    public void Run(WorldGenContext context)
    {
        foreach (var (cx, cy) in context.Map.AllChunkCoords())
        {
            var chunk = context.Map.GetChunk(cx, cy);
            for (int i = 0; i < chunk.Terrain.Length; i++)
            {
                if (chunk.Terrain[i] is TileId.ShallowWater or TileId.DeepWater)
                    continue;

                var e = chunk.Elevation[i];
                var m = chunk.Moisture[i];
                var t = chunk.Temperature[i];

                // Simple rules, placeholder:
                if (t < 40) chunk.Terrain[i] = TileId.Snow;
                else if (m < 40) chunk.Terrain[i] = TileId.Sand;
                else if (e > 220) chunk.Terrain[i] = TileId.Rock;
                else chunk.Terrain[i] = TileId.Grass;
            }
        }
    }
}
