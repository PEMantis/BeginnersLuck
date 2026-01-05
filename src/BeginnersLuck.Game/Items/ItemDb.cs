using System.Collections.Generic;

namespace BeginnersLuck.Game.Items;

public sealed class ItemDb
{
    private readonly Dictionary<string, ItemDef> _defs = new();

    public ItemDb()
    {
        Add(new ItemDef(
            Id: "potion",
            Name: "Potion",
            Usable: true,
            Effect: UseEffect.HealHp,
            Amount: 20,
            Description: "Restores a small amount of HP.",
            Rarity: ItemRarity.Common
        ));

        Add(new ItemDef(
            Id: "healing_potion",
            Name: "Healing Potion",
            Usable: true,
            Effect: UseEffect.HealHp,
            Amount: 40,
            Description: "Restores a moderate amount of HP.",
            Rarity: ItemRarity.Uncommon
        ));

        Add(new ItemDef(
            Id: "slime_gel",
            Name: "Slime Gel",
            Usable: false,
            Effect: UseEffect.None,
            Amount: 0,
            Description: "Sticky and faintly warm.",
            Rarity: ItemRarity.Common
        ));

        Add(new ItemDef(
            Id: "herb",
            Name: "Herb",
            Usable: true,
            Effect: UseEffect.HealHp,
            Amount: 10,
            Description: "A bitter leaf that restores a little HP.",
            Rarity: ItemRarity.Common
        ));

        Add(new ItemDef(
            Id: "ring_tin",
            Name: "Tin Ring",
            Usable: false,
            Effect: UseEffect.None,
            Amount: 0,
            Description: "A cheap, slightly bent ring.",
            Rarity: ItemRarity.Uncommon
        ));
    }

    public void Add(ItemDef def)
        => _defs[def.Id] = def;

    public bool TryGet(string id, out ItemDef def)
    {
#pragma warning disable CS8601
        return _defs.TryGetValue(id, out def);
#pragma warning restore CS8601
    }

    // Prefer these for UI/Battle
    public string DisplayNameOf(string id)
        => _defs.TryGetValue(id, out var def) ? def.Name : id;

    public ItemRarity RarityOf(string id)
        => _defs.TryGetValue(id, out var def) ? def.Rarity : ItemRarity.Common;

    // Back-compat helpers (fine to keep)
    public string NameOf(string id)
        => _defs.TryGetValue(id, out var def) ? def.Name : id;

    public string DescOfOrFallback(string id)
    {
        if (_defs.TryGetValue(id, out var def) &&
            !string.IsNullOrWhiteSpace(def.Description))
        {
            return def.Description!;
        }

        return "No description available.";
    }

    public string UsePreviewOf(string id)
    {
        if (!_defs.TryGetValue(id, out var def))
            return "UNKNOWN.";

        if (!def.Usable || def.Effect == UseEffect.None)
            return "NOT USABLE.";

        return def.Effect switch
        {
            UseEffect.HealHp => $"RESTORES {def.Amount} HP.",
            _ => "EFFECT UNKNOWN."
        };
    }

    public bool IsUsable(string id)
        => _defs.TryGetValue(id, out var def) && def.Usable && def.Effect != UseEffect.None;
}
