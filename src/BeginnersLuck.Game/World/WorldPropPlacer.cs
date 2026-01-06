using System;
using BeginnersLuck.WorldGen.Data;
using Microsoft.Xna.Framework;

namespace BeginnersLuck.Game.World;

public static class WorldPropPlacer
{
    // Deterministic 0..1 from world seed + cell + salt
    public static float Noise01(int seed, int x, int y, int salt)
    {
        unchecked
        {
            uint h = (uint)seed;
            h ^= (uint)(x * 73856093);
            h ^= (uint)(y * 19349663);
            h ^= (uint)(salt * 83492791);
            h ^= h >> 16;
            h *= 0x7feb352d;
            h ^= h >> 15;
            h *= 0x846ca68b;
            h ^= h >> 16;
            return (h & 0x00FFFFFF) / (float)0x01000000;
        }
    }

    public static int Range(int seed, int x, int y, int salt, int min, int maxInclusive)
    {
        float t = Noise01(seed, x, y, salt);
        int span = (maxInclusive - min) + 1;
        return min + (int)(t * span);
    }

    public static Point JitterPx(int seed, int x, int y, int salt, int maxAbs)
    {
        int jx = Range(seed, x, y, salt + 11, -maxAbs, maxAbs);
        int jy = Range(seed, x, y, salt + 29, -maxAbs, maxAbs);
        return new Point(jx, jy);
    }

    public static bool ShouldPlace(float chance01, int seed, int x, int y, int salt)
        => Noise01(seed, x, y, salt) <= chance01;
}
