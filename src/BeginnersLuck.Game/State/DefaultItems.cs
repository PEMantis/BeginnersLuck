namespace BeginnersLuck.Game.Items;

public static class DefaultItems
{
    public static ItemDb Create()
    {
        var db = new ItemDb();

        // Add as you go; unknown IDs will fall back to itemId.
        db.Add(new ItemDef(Id: "gel", Name: "Slime Gel", true, UseEffect.None, 1));
        db.Add(new ItemDef(Id: "herb", Name: "Green Herb", true, UseEffect.HealHp, 1));
        db.Add(new ItemDef(Id: "turd", Name: "Poo", true, UseEffect.None, 1));

        return db;
    }
}
