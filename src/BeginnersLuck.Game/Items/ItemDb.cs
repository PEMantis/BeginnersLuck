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
            Description: "Restores a small amount of HP."
        ));

        Add(new ItemDef(
            Id: "slime_gel",
            Name: "Slime Gel",
            Usable: false,
            Effect: UseEffect.None,
            Amount: 0,
            Description: "Sticky and faintly warm."
        ));
    }

    public void Add(ItemDef def)
        => _defs[def.Id] = def;

    public bool TryGet(string id, out ItemDef def)
        => _defs.TryGetValue(id, out def);

    public string NameOf(string id)
        => _defs.TryGetValue(id, out var def) ? def.Name : id;

    // 🔽 THIS IS THE METHOD YOU ASKED ABOUT
    public string DescOfOrFallback(string id)
    {
        if (_defs.TryGetValue(id, out var def) &&
            !string.IsNullOrWhiteSpace(def.Description))
        {
            return def.Description!;
        }

        return "No description available.";
    }
}
