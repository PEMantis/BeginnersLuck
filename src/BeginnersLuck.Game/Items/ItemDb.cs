using System;
using System.Collections.Generic;
using BeginnersLuck.Game.Assets;

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
            Rarity: ItemRarity.Common,
            Category: ItemCategory.Consumable,
            MaxStack: 20,
            IconId: AssetKeys.Items.HealthHerb,
            DebugValue: 10
        ));

        Add(new ItemDef(
            Id: "healing_potion",
            Name: "Healing Potion",
            Usable: true,
            Effect: UseEffect.HealHp,
            Amount: 40,
            Description: "Restores a moderate amount of HP.",
            Rarity: ItemRarity.Uncommon,
            Category: ItemCategory.Consumable,
            MaxStack: 10,
            IconId: AssetKeys.Items.HealthHerb,
            DebugValue: 22
        ));

        Add(new ItemDef(
            Id: "slime_gel",
            Name: "Slime Gel",
            Usable: false,
            Effect: UseEffect.None,
            Amount: 0,
            Description: "Sticky and faintly warm.",
            Rarity: ItemRarity.Common,
            Category: ItemCategory.Material,
            MaxStack: 99,
            IconId: AssetKeys.Items.SlimeGel,
            DebugValue: 5
        ));

        Add(new ItemDef(
            Id: "herb",
            Name: "Herb",
            Usable: true,
            Effect: UseEffect.HealHp,
            Amount: 10,
            Description: "A bitter leaf that restores a little HP.",
            Rarity: ItemRarity.Common,
            Category: ItemCategory.Consumable,
            MaxStack: 30,
            IconId: AssetKeys.Items.HealthHerb,
            DebugValue: 4
        ));

        Add(new ItemDef(
            Id: "ring_tin",
            Name: "Tin Ring",
            Usable: false,
            Effect: UseEffect.None,
            Amount: 0,
            Description: "A cheap, slightly bent ring.",
            Rarity: ItemRarity.Uncommon,
            Category: ItemCategory.Misc,
            MaxStack: 30,
            IconId: AssetKeys.Items.OldCoin,
            DebugValue: 8
        ));

        // Inventory + Loot v1 core definitions.
        Add(new ItemDef(
            Id: "berries",
            Name: "Berries",
            Usable: false,
            Effect: UseEffect.None,
            Amount: 0,
            Description: "A handful of tart berries.",
            Rarity: ItemRarity.Common,
            Category: ItemCategory.Resource,
            MaxStack: 30,
            IconId: AssetKeys.Items.Berries,
            DebugValue: 1
        ));

        Add(new ItemDef(
            Id: "wood",
            Name: "Wood",
            Usable: false,
            Effect: UseEffect.None,
            Amount: 0,
            Description: "Useful for basic crafting and repairs.",
            Rarity: ItemRarity.Common,
            Category: ItemCategory.Material,
            MaxStack: 99,
            IconId: AssetKeys.Items.Wood,
            DebugValue: 2
        ));

        Add(new ItemDef(
            Id: "stone",
            Name: "Stone",
            Usable: false,
            Effect: UseEffect.None,
            Amount: 0,
            Description: "Rough stones gathered from the ground.",
            Rarity: ItemRarity.Common,
            Category: ItemCategory.Material,
            MaxStack: 99,
            IconId: AssetKeys.Items.Stone,
            DebugValue: 2
        ));

        Add(new ItemDef(
            Id: "old_coin",
            Name: "Old Coin",
            Usable: false,
            Effect: UseEffect.None,
            Amount: 0,
            Description: "An old coin with faded markings.",
            Rarity: ItemRarity.Uncommon,
            Category: ItemCategory.Misc,
            MaxStack: 99,
            IconId: AssetKeys.Items.OldCoin,
            DebugValue: 6
        ));

        Add(new ItemDef(
            Id: "health_herb",
            Name: "Health Herb",
            Usable: true,
            Effect: UseEffect.HealHp,
            Amount: 15,
            Description: "A restorative herb that heals minor wounds.",
            Rarity: ItemRarity.Common,
            Category: ItemCategory.Consumable,
            MaxStack: 30,
            IconId: AssetKeys.Items.HealthHerb,
            DebugValue: 6
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

    public bool IsKnown(string id)
        => !string.IsNullOrWhiteSpace(id) && _defs.ContainsKey(id);

    public int MaxStackOf(string id)
        => _defs.TryGetValue(id, out var def) ? Math.Max(1, def.MaxStack) : 99;

    public ItemCategory CategoryOf(string id)
        => _defs.TryGetValue(id, out var def) ? def.Category : ItemCategory.Misc;

    public string IconIdOf(string id)
        => _defs.TryGetValue(id, out var def)
            ? (!string.IsNullOrWhiteSpace(def.IconId) ? def.IconId! : def.Id)
            : id;
}
