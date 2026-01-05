using Microsoft.Xna.Framework;
using BeginnersLuck.Game.Items;

namespace BeginnersLuck.Game.UI;

public static class RarityColors
{
    public static Color For(ItemRarity r) => r switch
    {
        ItemRarity.Common    => Color.White * 0.85f,
        ItemRarity.Uncommon  => new Color(120, 220, 120),
        ItemRarity.Rare      => new Color(100, 170, 255),
        ItemRarity.Epic      => new Color(190, 120, 255),
        ItemRarity.Legendary => new Color(255, 170, 70),
        _ => Color.White
    };

    public static string Label(ItemRarity r) => r switch
    {
        ItemRarity.Common => "",
        _ => $"[{r.ToString().ToUpperInvariant()}]"
    };
}
