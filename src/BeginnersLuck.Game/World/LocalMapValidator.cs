using System;
using System.Linq;
using System.Reflection;
using BeginnersLuck.WorldGen.Local;

namespace BeginnersLuck.Game.World;

public static class LocalMapValidator
{
    public sealed record Report(
        bool Playable,
        string Reason,
        int WalkableCount,
        int LargestRegion,
        bool LargestTouchesEdge,
        int RoadCount
    );

    public static Report Validate(object data, LocalMapPurpose purpose)
    {
        // We use reflection so this validator does NOT depend on LocalMapData's exact member names.
        // That prevents compile breaks when LocalMapData changes.

        int w = TryGetInt(data, "Width", "W", "MapWidth", "Size");
        int h = TryGetInt(data, "Height", "H", "MapHeight", "Size");

        // Try to locate arrays that indicate solid / walkable.
        // Common candidates across versions:
        // - bool[] Solid
        // - bool[] Solids
        // - byte[] Solid
        // - ushort[] Flags + a known "solid" bit (we can't know your bit here, so only use if explicitly present)
        var solidBools = TryGetArray<bool>(data, "Solid", "Solids", "IsSolid", "Collision");
        var solidBytes = solidBools == null ? TryGetArray<byte>(data, "Solid", "Solids") : null;

        // Optional: roads counter if you have a road mask/array.
        // We'll try common names; if absent, RoadCount = 0 and Road maps still validate like base maps.
        var roadBools = TryGetArray<bool>(data, "Road", "Roads", "IsRoad");
        var roadBytes = roadBools == null ? TryGetArray<byte>(data, "Road", "Roads") : null;

        int cellCount =
            solidBools?.Length ??
            solidBytes?.Length ??
            roadBools?.Length ??
            roadBytes?.Length ??
            TryGetArray<int>(data, "Tiles", "Terrain", "Cells")?.Length ??
            0;

        // If width/height aren't available, derive them if possible
        if (w <= 0 || h <= 0)
        {
            if (cellCount > 0)
            {
                // Assume square if Size not present.
                int side = (int)MathF.Round(MathF.Sqrt(cellCount));
                if (side * side == cellCount)
                {
                    w = side;
                    h = side;
                }
                else
                {
                    // Last resort: treat as 1D
                    w = cellCount;
                    h = 1;
                }
            }
            else
            {
                // We can't reason about anything. Do not block generation.
                return new Report(
                    Playable: true,
                    Reason: "OK (validator could not inspect map shape)",
                    WalkableCount: 0,
                    LargestRegion: 0,
                    LargestTouchesEdge: true,
                    RoadCount: 0
                );
            }
        }

        // Compute walkable count
        int walkable = 0;
        if (solidBools != null)
        {
            for (int i = 0; i < solidBools.Length; i++)
                if (!solidBools[i]) walkable++;
        }
        else if (solidBytes != null)
        {
            for (int i = 0; i < solidBytes.Length; i++)
                if (solidBytes[i] == 0) walkable++;
        }
        else
        {
            // No solidity info => don't fail maps because of validator limitations.
            return new Report(
                Playable: true,
                Reason: "OK (no Solid array found)",
                WalkableCount: 0,
                LargestRegion: 0,
                LargestTouchesEdge: true,
                RoadCount: CountRoads(roadBools, roadBytes)
            );
        }

        // For now we keep it simple: "largest region" and "touches edge" are conservative approximations.
        // If you later want strict region-floodfill, we can add it against the discovered arrays.
        int largest = walkable; // conservative (won't false-fail)
        bool touchesEdge = HasAnyWalkableOnEdge(w, h, solidBools, solidBytes);

        int roads = CountRoads(roadBools, roadBytes);

        // Thresholds (tuned gentle, based on area)
        int area = w * h;
        int minWalkable = Math.Max(200, area / 10);
        int minLargest = Math.Max(180, area / 12);

        bool basePlayable = walkable >= minWalkable && largest >= minLargest && touchesEdge;

        // Purpose-aware rules.
        // For now Road/Ruins follow basePlayable (same as Town), so you can ship POIs now.
        bool playable = purpose switch
        {
            LocalMapPurpose.Town => basePlayable,
            LocalMapPurpose.Road => basePlayable,   // tighten later: require roads >= N if you want
            LocalMapPurpose.Ruins => basePlayable,  // tighten later: require ruin features if you want
            _ => basePlayable
        };

        string reason = playable
            ? "OK"
            : $"UNPLAYABLE ({purpose})";

        return new Report(
            Playable: playable,
            Reason: reason,
            WalkableCount: walkable,
            LargestRegion: largest,
            LargestTouchesEdge: touchesEdge,
            RoadCount: roads
        );
    }

    // --- helpers ---

    private static int CountRoads(bool[]? roadBools, byte[]? roadBytes)
    {
        if (roadBools != null)
        {
            int n = 0;
            for (int i = 0; i < roadBools.Length; i++)
                if (roadBools[i]) n++;
            return n;
        }

        if (roadBytes != null)
        {
            int n = 0;
            for (int i = 0; i < roadBytes.Length; i++)
                if (roadBytes[i] != 0) n++;
            return n;
        }

        return 0;
    }

    private static bool HasAnyWalkableOnEdge(int w, int h, bool[]? solidBools, byte[]? solidBytes)
    {
        bool IsWalkable(int x, int y)
        {
            int i = x + y * w;

            if (solidBools != null)
                return i >= 0 && i < solidBools.Length && !solidBools[i];

            if (solidBytes != null)
                return i >= 0 && i < solidBytes.Length && solidBytes[i] == 0;

            return true;
        }

        for (int x = 0; x < w; x++)
        {
            if (IsWalkable(x, 0) || IsWalkable(x, h - 1))
                return true;
        }

        for (int y = 0; y < h; y++)
        {
            if (IsWalkable(0, y) || IsWalkable(w - 1, y))
                return true;
        }

        return false;
    }

    private static int TryGetInt(object obj, params string[] names)
    {
        foreach (var n in names)
        {
            var p = obj.GetType().GetProperty(n, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null && p.PropertyType == typeof(int))
            {
                var v = (int)p.GetValue(obj)!;
                if (v > 0) return v;
            }

            var f = obj.GetType().GetField(n, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null && f.FieldType == typeof(int))
            {
                var v = (int)f.GetValue(obj)!;
                if (v > 0) return v;
            }
        }

        return 0;
    }

    private static T[]? TryGetArray<T>(object obj, params string[] names)
    {
        foreach (var n in names)
        {
            var p = obj.GetType().GetProperty(n, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null && typeof(T[]).IsAssignableFrom(p.PropertyType))
                return (T[]?)p.GetValue(obj);

            var f = obj.GetType().GetField(n, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null && typeof(T[]).IsAssignableFrom(f.FieldType))
                return (T[]?)f.GetValue(obj);
        }

        return null;
    }
}
