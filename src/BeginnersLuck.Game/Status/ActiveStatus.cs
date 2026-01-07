using System;

namespace BeginnersLuck.Game.Status;

public sealed class ActiveStatus
{
    public string Id { get; }
    public string SourceTag { get; } // optional: "skill:haste", "poison", etc.

    public StatModifier Mods { get; }

    public int RemainingTurns { get; private set; }

    public ActiveStatus(string id, StatModifier mods, int durationTurns, string sourceTag = "")
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Status id required.", nameof(id));
        if (durationTurns <= 0) throw new ArgumentOutOfRangeException(nameof(durationTurns), "Duration must be > 0.");

        Id = id;
        SourceTag = sourceTag ?? "";
        Mods = mods ?? throw new ArgumentNullException(nameof(mods));
        RemainingTurns = durationTurns;
    }

    public void TickDown()
    {
        RemainingTurns--;
    }

    public bool Expired => RemainingTurns <= 0;
}
