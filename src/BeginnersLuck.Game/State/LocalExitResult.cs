using Microsoft.Xna.Framework;
using BeginnersLuck.WorldGen.Local;
using BeginnersLuck.Game.World;

namespace BeginnersLuck.Game.State;

/// <summary>
/// What happened when we exited a local map back to the world.
/// WorldMapScene consumes this exactly once on resume.
/// </summary>
public sealed record LocalExitResult(
    int FromWorldX,
    int FromWorldY,
    Dir ExitDir,
    LocalMapPurpose Purpose,
    string LocalBinPath,
    Point LocalExitCell
);
