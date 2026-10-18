using Medzo.CatalogueInventory.Domain.Inventory;
using Xunit;

namespace Medzo.CatalogueInventory.UnitTests;

public sealed class InventoryBoundaryTests
{
    [Theory]
    [InlineData(4, true)]
    [InlineData(5, false)]
    [InlineData(6, false)]
    public void Low_stock_uses_strict_threshold(int quantity, bool expected)
    {
        var inventory = new InventoryItem(Guid.NewGuid(), 5);
        inventory.ChangeStock(quantity, StockMovementType.BatchReceived, "purchase", "PO-1");
        Assert.Equal(expected, inventory.IsLowStock);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-6)]
    public void Rejected_stock_change_preserves_balance_version_and_audit(int delta)
    {
        var inventory = new InventoryItem(Guid.NewGuid());
        inventory.ChangeStock(5, StockMovementType.BatchReceived, "purchase", "PO-1");
        var version = inventory.Version;

        if (delta == 0)
            Assert.Throws<ArgumentException>(() => inventory.ChangeStock(delta, StockMovementType.SaleDispensed, "sale", "SALE-1"));
        else
            Assert.Throws<InvalidOperationException>(() => inventory.ChangeStock(delta, StockMovementType.SaleDispensed, "sale", "SALE-1"));

        Assert.Equal(5, inventory.QuantityOnHand);
        Assert.Equal(version, inventory.Version);
        Assert.Single(inventory.Movements);
    }

    [Fact]
    public void Threshold_change_updates_version_only_when_value_changes()
    {
        var inventory = new InventoryItem(Guid.NewGuid(), 5);
        var version = inventory.Version;
        inventory.SetReorderThreshold(5);
        Assert.Equal(version, inventory.Version);
        inventory.SetReorderThreshold(6);
        Assert.Equal(version + 1, inventory.Version);
        Assert.Throws<ArgumentOutOfRangeException>(() => inventory.SetReorderThreshold(-1));
        Assert.Equal(6, inventory.ReorderThreshold);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(11)]
    public void Invalid_consumption_preserves_batch_quantity(int quantity)
    {
        var batch = Batch();
        Assert.Throws<InvalidOperationException>(() => batch.Consume(quantity));
        Assert.Equal(10, batch.RemainingQuantity);
    }

    [Fact]
    public void Receive_after_partial_sale_keeps_initial_and_remaining_quantities_distinct()
    {
        var batch = Batch();
        batch.Consume(4);
        batch.Receive(3);
        Assert.Equal(13, batch.InitialQuantity);
        Assert.Equal(9, batch.RemainingQuantity);
        batch.Consume(9);
        Assert.Equal(0, batch.RemainingQuantity);
    }

    private static StockBatch Batch() => new(Guid.NewGuid(), "LOT-1", null,
        DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)), 10);
}
