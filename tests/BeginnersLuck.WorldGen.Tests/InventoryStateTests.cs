using BeginnersLuck.Game.Items;
using BeginnersLuck.Game.State;
using System.Linq;

namespace BeginnersLuck.WorldGen.Tests;

public sealed class InventoryStateTests
{
    [Fact]
    public void AddItem_StacksAcrossMaxStackBoundaries()
    {
        var db = new ItemDb();
        var inv = new InventoryState();

        bool ok = inv.AddItem("berries", 65, db);

        Assert.True(ok);
        Assert.Equal(65, inv.GetQuantity("berries"));

        var stacks = inv.GetAllStacks(db)
            .Where(s => s.ItemId == "berries")
            .Select(s => s.Quantity)
            .ToArray();

        Assert.Equal(new[] { 30, 30, 5 }, stacks);
    }

    [Fact]
    public void RemoveItem_DecreasesQuantityAndPreventsNegative()
    {
        var db = new ItemDb();
        var inv = new InventoryState();

        inv.AddItem("wood", 4, db);

        Assert.True(inv.RemoveItem("wood", 3));
        Assert.Equal(1, inv.GetQuantity("wood"));

        Assert.False(inv.RemoveItem("wood", 2));
        Assert.Equal(1, inv.GetQuantity("wood"));
    }

    [Fact]
    public void RemoveItem_InvalidQuantity_ReturnsFalse()
    {
        var db = new ItemDb();
        var inv = new InventoryState();

        inv.AddItem("stone", 2, db);

        Assert.False(inv.RemoveItem("stone", 0));
        Assert.False(inv.RemoveItem("stone", -1));
        Assert.Equal(2, inv.GetQuantity("stone"));
    }

    [Fact]
    public void AddItem_UnknownId_IsHandledSafely()
    {
        var db = new ItemDb();
        var inv = new InventoryState();

        bool ok = inv.AddItem("does_not_exist", 3, db);

        Assert.False(ok);
        Assert.Equal(0, inv.GetQuantity("does_not_exist"));
    }
}
