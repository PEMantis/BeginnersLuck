using Microsoft.Xna.Framework;

namespace BeginnersLuck.Game.World;

public readonly record struct InteractionTargetInfo(
    GameEntity? HighlightEntity,
    Point HighlightTile,
    string Prompt,
    string Label,
    bool IsAttackPrompt)
{
    public bool HasTarget => HighlightEntity != null;
}
