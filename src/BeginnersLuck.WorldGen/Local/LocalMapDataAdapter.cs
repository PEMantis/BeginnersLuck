using System;
using BeginnersLuck.Engine.World;
using BeginnersLuck.WorldGen.Data;
using BeginnersLuck.WorldGen.Local;
using BeginnersLuck.WorldGen.Local.Export;

namespace BeginnersLuck.WorldGen;

public static class LocalMapDataAdapter
{
    public static LocalMapMeta ToData(LocalMap m, LocalMapPurpose purpose, BiomeId biome, EdgePortals portals, (int X, int Y)? townCenter)
    {
        if (m == null) throw new ArgumentNullException(nameof(m));

        int n = m.Size;
        int count = n * n;

        if (m.Elevation.Length != count) throw new InvalidOperationException("Elevation length mismatch.");
        if (m.Moisture.Length != count) throw new InvalidOperationException("Moisture length mismatch.");
        if (m.Temperature.Length != count) throw new InvalidOperationException("Temperature length mismatch.");
        if (m.Terrain.Length != count) throw new InvalidOperationException("Terrain length mismatch.");
        if (m.Flags.Length != count) throw new InvalidOperationException("Flags length mismatch.");

        // Copy arrays (avoid aliasing)
        var elevation = new byte[count];
        var moisture = new byte[count];
        var temperature = new byte[count];
        Array.Copy(m.Elevation, elevation, count);
        Array.Copy(m.Moisture, moisture, count);
        Array.Copy(m.Temperature, temperature, count);

        var terrain = new TileId[count];
        var flags = new TileFlags[count];
        Array.Copy(m.Terrain, terrain, count);
        Array.Copy(m.Flags, flags, count);

        return new LocalMapMeta(
            size: n,
            seed: m.Seed,
            wx: m.WorldX,
            wy: m.WorldY,
            purpose: purpose,
            biome: biome,
            elevation: elevation,
            moisture: moisture,
            temperature: temperature,
            terrain: terrain,
            flags: flags,
            portals: portals,
            townCenter: townCenter
        );
    }
}
