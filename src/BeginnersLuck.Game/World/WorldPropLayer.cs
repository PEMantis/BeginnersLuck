using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace BeginnersLuck.Game.World;

public sealed class WorldPropLayer
{
    private readonly List<WorldProp> _props = new();
    public IReadOnlyList<WorldProp> Props => _props;

    public void Clear() => _props.Clear();

    public void GenerateForRect(int seed, Rectangle tileRect, Func<Point, ZoneId> zoneAt)
    {
        _props.Clear();
        var rng = new Random(seed);

        for (int y = tileRect.Top; y < tileRect.Bottom; y++)
        for (int x = tileRect.Left; x < tileRect.Right; x++)
        {
            var p = new Point(x, y);
            var z = zoneAt(p);

            // Deterministic-ish per tile: mix seed + coordinates
            // (Keeps results stable across sessions without storing every prop)
            int h = Hash(seed, x, y);
            var local = new Random(h);

            switch (z)
            {
                case ZoneId.Forest:
                {
                    // More trees
                    if (local.Next(0, 100) < 18)
                        _props.Add(new WorldProp("tree_oak", p, BlocksMove: true));
                    else if (local.Next(0, 100) < 6)
                        _props.Add(new WorldProp("rock", p, BlocksMove: false));
                    break;
                }

                case ZoneId.Grasslands:
                {
                    if (local.Next(0, 100) < 6)
                        _props.Add(new WorldProp("tree_pine", p, BlocksMove: true));
                    else if (local.Next(0, 100) < 5)
                        _props.Add(new WorldProp("rock", p, BlocksMove: false));
                    break;
                }

                case ZoneId.Mountains:
                {
                    if (local.Next(0, 100) < 20)
                        _props.Add(new WorldProp("mountain_peak", p, BlocksMove: true));
                    else if (local.Next(0, 100) < 8)
                        _props.Add(new WorldProp("rock", p, BlocksMove: true));
                    break;
                }

                case ZoneId.Ruins:
                {
                    if (local.Next(0, 100) < 14)
                        _props.Add(new WorldProp("ruin_pillar", p, BlocksMove: true));
                    else if (local.Next(0, 100) < 10)
                        _props.Add(new WorldProp("ruin_rubble", p, BlocksMove: false));
                    break;
                }

                default:
                    break;
            }
        }
    }

    private static int Hash(int seed, int x, int y)
    {
        unchecked
        {
            int h = seed;
            h = h * 31 + x;
            h = h * 31 + y;
            h ^= (h << 13);
            h ^= (h >> 17);
            h ^= (h << 5);
            return h;
        }
    }
}
