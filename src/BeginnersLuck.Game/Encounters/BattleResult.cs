using System;
using System.Collections.Generic;
using BeginnersLuck.Game.Services;

namespace BeginnersLuck.Game.Battles;

public enum BattleOutcome
{
    Victory,
    Defeat,
    Fled
}

public readonly record struct LootLine(string ItemId, int Qty);

public sealed class BattleResult
{
    public string EncounterId { get; init; } = "";
    public string EncounterName { get; init; } = "";

    public BattleOutcome Outcome { get; init; }

    public int Xp { get; init; }
    public int Gold { get; init; }

    public IReadOnlyList<LootLine> Loot { get; init; } = Array.Empty<LootLine>();

    // Optional: set when XP is applied so UI can display it.
    public PlayerXpReport? XpReport { get; internal set; }

    // Guard against double-apply
    public bool Applied { get; internal set; }
}
