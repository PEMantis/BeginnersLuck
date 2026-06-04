using Microsoft.Xna.Framework;

namespace BeginnersLuck.Game.World;

public static class InteractionPromptBuilder
{
    public static InteractionTargetInfo Build(EntityManager entities, Point playerTile, Point facing)
    {
        var attackTarget = LocalMapCombatSystem.FindAdjacentEnemy(entities, playerTile, facing);
        if (attackTarget != null)
        {
            return new InteractionTargetInfo(
                HighlightEntity: attackTarget,
                HighlightTile: attackTarget.Tile,
                Prompt: $"F: Attack {attackTarget.DisplayName}",
                Label: attackTarget.DisplayName,
                IsAttackPrompt: true);
        }

        var interactTarget = InteractionSystem.FindInteractionTarget(entities, playerTile, facing);
        if (interactTarget == null)
            return default;

        string prompt = interactTarget.Type switch
        {
            GameEntityType.ResourceNode => $"E: Gather {interactTarget.DisplayName}",
            GameEntityType.Chest => $"E: Open {interactTarget.DisplayName}",
            GameEntityType.Door => $"E: Inspect {interactTarget.DisplayName}",
            GameEntityType.Enemy => $"E: Inspect {interactTarget.DisplayName}",
            _ => $"E: Interact {interactTarget.DisplayName}",
        };

        return new InteractionTargetInfo(
            HighlightEntity: interactTarget,
            HighlightTile: interactTarget.Tile,
            Prompt: prompt,
            Label: interactTarget.DisplayName,
            IsAttackPrompt: false);
    }
}
