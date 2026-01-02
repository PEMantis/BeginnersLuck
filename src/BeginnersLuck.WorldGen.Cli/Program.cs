using BeginnersLuck.WorldGen;
using BeginnersLuck.WorldGen.Generation;
using BeginnersLuck.WorldGen.Serialization;
using BeginnersLuck.WorldGen.Cli;

static int GetInt(string[] args, string key, int fallback)
{
    int idx = Array.IndexOf(args, key);
    return (idx >= 0 && idx + 1 < args.Length && int.TryParse(args[idx + 1], out var v)) ? v : fallback;
}

static float GetFloat(string[] args, string key, float fallback)
{
    int idx = Array.IndexOf(args, key);
    return (idx >= 0 && idx + 1 < args.Length && float.TryParse(args[idx + 1], out var v)) ? v : fallback;
}

static string GetString(string[] args, string key, string fallback)
{
    int idx = Array.IndexOf(args, key);
    return (idx >= 0 && idx + 1 < args.Length) ? args[idx + 1] : fallback;
}

static bool Has(string[] args, string key) => Array.IndexOf(args, key) >= 0;

static void PrintHelp()
{
    Console.WriteLine("""
BeginnerLuck.WorldGen.Cli

Usage:
  worldgen --seed 12345 --out ./Worlds/test --size 512 --chunk 32 --water 0.55 --png

Options:
  --seed    <int>     World seed (default 12345)
  --out     <path>    Output folder (default ./Worlds/out)
  --size    <int>     World width/height (square) (default 512)
  --chunk   <int>     Chunk size (default 32)
  --water   <float>   Water percent 0..1 (default 0.55)
  --png               Write debug pngs (elevation/moisture/temp/terrain)
  --help              Show help
""");
}

var argsList = args ?? Array.Empty<string>();
if (Has(argsList, "--help") || Has(argsList, "-h"))
{
    PrintHelp();
    return 0;
}

int seed = GetInt(argsList, "--seed", 12345);
int size = GetInt(argsList, "--size", 512);
int chunk = GetInt(argsList, "--chunk", 32);
float water = GetFloat(argsList, "--water", 0.55f);
string outDir = GetString(argsList, "--out", "./Worlds/out");
bool writePng = Has(argsList, "--png");

var settings = new WorldGenSettings
{
    WaterPercent = water,
};

var request = new WorldGenRequest(
    Width: size,
    Height: size,
    ChunkSize: chunk,
    Seed: seed,
    Settings: settings
);

IWorldGenerator generator = new WorldGenerator();
var world = generator.Generate(request);

Directory.CreateDirectory(outDir);

// JSON metadata for now (still useful)
WorldWriter.WriteJson(Path.Combine(outDir, "world.json"), world);

if (writePng)
{
    WorldPngDump.WriteAll(outDir, world);
}

Console.WriteLine($"Generated world: {size}x{size}, chunk {chunk}, seed {seed}, water {water:0.00}");
Console.WriteLine($"Wrote: {Path.GetFullPath(Path.Combine(outDir, "world.json"))}");
if (writePng) Console.WriteLine("Wrote: elevation.png, moisture.png, temperature.png, terrain.png");

return 0;
