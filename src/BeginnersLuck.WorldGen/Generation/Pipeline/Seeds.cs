using System.Security.Cryptography;
using System.Text;

namespace BeginnersLuck.WorldGen.Generation.Pipeline;

public static class Seeds
{
    public static int Derive(int rootSeed, string label, int a = 0, int b = 0)
    {
        var bytes = Encoding.UTF8.GetBytes($"{rootSeed}|{label}|{a}|{b}");
        var hash = SHA256.HashData(bytes);
        return BitConverter.ToInt32(hash, 0);
    }
}
