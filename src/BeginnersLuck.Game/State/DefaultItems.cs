namespace BeginnersLuck.Game.Items;

public static class DefaultItems
{
    public static ItemDb Create()
    {
        var db = new ItemDb();

        // Add as you go; unknown IDs will fall back to itemId.
        db.Add("gel", "Slime Gel");
        db.Add("herb", "Green Herb");
        db.Add("ring_tin", "Tin Ring");

        return db;
    }
}
