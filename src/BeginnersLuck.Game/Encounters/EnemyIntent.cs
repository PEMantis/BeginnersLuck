using BeginnersLuck.Game.World;
using Microsoft.Xna.Framework;

namespace BeginnersLuck.Game.Encounters;

public readonly record struct EncounterIntent(
    EncounterDef Encounter,
    ZoneInfo Zone,
    Point Cell
);
