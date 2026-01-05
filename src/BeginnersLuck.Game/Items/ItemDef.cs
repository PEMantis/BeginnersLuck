namespace BeginnersLuck.Game.Items;

public sealed record ItemDef(
    string Id,
    string Name,
    bool Usable,
    UseEffect Effect,
    int Amount,
    string? Description = null,
    ItemRarity Rarity = ItemRarity.Common
);
