using BeginnersLuck.WorldGen.Data;

namespace BeginnersLuck.WorldGen.Generation.Pipeline;

public sealed class WorldGenContext
{
    public WorldGenRequest Request { get; }
    public WorldMap Map { get; }
    public int RootSeed => Request.Seed;
    private readonly Dictionary<string, object> _data = new();
    public void Set<T>(string key, T value) where T : notnull => _data[key] = value;
    public T Get<T>(string key) => (T)_data[key];
    
    public bool TryGet<T>(string key, out T value)
    {
        if (_data.TryGetValue(key, out var obj) && obj is T t) { value = t; return true; }
        value = default!;
        return false;
    }

    public WorldGenContext(WorldGenRequest request, WorldMap map)
    {
        Request = request;
        Map = map;
    }

    public int SeedFor(string label, int a = 0, int b = 0) =>
        Seeds.Derive(Request.Seed, label, a, b);
}
