using System;
using BeginnersLuck.Game.World;

namespace BeginnersLuck.Game.Encounters;

public interface IEncounterSource
{
    EncounterDef PickEncounter(ZoneInfo zone, Random rng);
}
