using System;
using BeginnersLuck.Engine.Content;

namespace BeginnersLuck.Game.Skills;

public static class SkillDbLoader
{
    /// <summary>
    /// Loads skills from raw content path. Keep this small and boring.
    /// </summary>
    public static SkillDb LoadFromRaw(RawContent raw, string relativePath)
    {
        var json = raw.LoadText(relativePath);

        var db = new SkillDb();
        db.LoadFromJson(json, relativePath);
        return db;
    }
}

/// <summary>
/// Minimal contract so SkillDb isn't coupled to your specific RawContent type.
/// If you already have IRawContent, delete this and use yours.
/// </summary>
public interface IRawContent
{
    string ReadAllText(string path);
}
