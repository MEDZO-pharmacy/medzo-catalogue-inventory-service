using Medzo.CatalogueInventory.Domain.Inventory;
using Xunit;

namespace Medzo.CatalogueInventory.UnitTests;

public sealed class InventoryItemTests
{
    [Fact]
    public void Constructor_StoresMedicineAndInitialThreshold()
    {
        var medicineId = Guid.NewGuid();

        var item = new InventoryItem(medicineId, 5);

        Assert.Equal(medicineId, item.MedicineId);
        Assert.Equal(5, item.ReorderThreshold);
        Assert.Equal(1, item.Version);
        Assert.True(item.IsLowStock);
    }

    [Fact]
    public void SetReorderThreshold_WithNegativeValue_ThrowsForValue()
    {
        var item = new InventoryItem(Guid.NewGuid());

        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => item.SetReorderThreshold(-1));

        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void SetReorderThreshold_WithSameValue_DoesNotIncrementVersion()
    {
        var item = new InventoryItem(Guid.NewGuid(), 5);
        var originalVersion = item.Version;

        item.SetReorderThreshold(5);

        Assert.Equal(originalVersion, item.Version);
    }

    [Fact]
    public void SetReorderThreshold_WithNewValue_UpdatesThresholdAndVersion()
    {
        var item = new InventoryItem(Guid.NewGuid(), 5);
        var originalVersion = item.Version;

        item.SetReorderThreshold(8);

        Assert.Equal(8, item.ReorderThreshold);
        Assert.Equal(originalVersion + 1, item.Version);
    }

    [Fact]
    public void MarkCatalogueChanged_IncrementsVersionOnce()
    {
        var item = new InventoryItem(Guid.NewGuid());
        var originalVersion = item.Version;

        item.MarkCatalogueChanged();

        Assert.Equal(originalVersion + 1, item.Version);
    }

    [Fact]
    public void ChangeStock_IncreasesBalanceAndCreatesCompleteAuditMovement()
    {
        var item = new InventoryItem(Guid.NewGuid(), 5);
        var batchId = Guid.NewGuid();
        var originalVersion = item.Version;
        var before = DateTime.UtcNow;

        var movement = item.ChangeStock(
            10,
            StockMovementType.BatchReceived,
            "purchase-order",
            "PO-100",
            batchId,
            12.50m,
            "pharmacist.one");

        var after = DateTime.UtcNow;
        Assert.Equal(10, item.QuantityOnHand);
        Assert.Equal(originalVersion + 1, item.Version);
        Assert.False(item.IsLowStock);
        Assert.Same(movement, Assert.Single(item.Movements));
        Assert.Equal(item.Id, movement.InventoryItemId);
        Assert.Equal(batchId, movement.StockBatchId);
        Assert.Equal(StockMovementType.BatchReceived, movement.Type);
        Assert.Equal(10, movement.QuantityDelta);
        Assert.Equal(0, movement.QuantityBefore);
        Assert.Equal(10, movement.QuantityAfter);
        Assert.Equal("purchase-order", movement.SourceType);
        Assert.Equal("PO-100", movement.SourceId);
        Assert.Equal(12.50m, movement.UnitPrice);
        Assert.Equal("pharmacist.one", movement.PharmacistUsername);
        Assert.InRange(movement.OccurredAtUtc, before, after);
    }

    [Fact]
    public void ChangeStock_WithZeroDelta_ThrowsClearError()
    {
        var item = new InventoryItem(Guid.NewGuid());

        var exception = Assert.Throws<ArgumentException>(() => item.ChangeStock(
            0, StockMovementType.AdjustmentIncrease, "adjustment", "ADJ-1"));

        Assert.Equal("Quantity change cannot be zero.", exception.Message);
    }

    [Fact]
    public void ChangeStock_RejectsNegativeBalanceWithClearError()
    {
        var item = new InventoryItem(Guid.NewGuid());

        var exception = Assert.Throws<InvalidOperationException>(() => item.ChangeStock(
            -1, StockMovementType.SaleDispensed, "sale", "SALE-1"));

        Assert.Equal("Insufficient stock.", exception.Message);
        Assert.Empty(item.Movements);
    }

    [Fact]
    public void ChangeStock_AllowsBalanceToReachExactlyZero()
    {
        var item = new InventoryItem(Guid.NewGuid(), 1);
        item.ChangeStock(1, StockMovementType.BatchReceived, "batch", "B-1");

        var movement = item.ChangeStock(-1, StockMovementType.SaleDispensed, "sale", "S-1");

        Assert.Equal(0, item.QuantityOnHand);
        Assert.Equal(1, movement.QuantityBefore);
        Assert.Equal(0, movement.QuantityAfter);
        Assert.True(item.IsLowStock);
    }

    [Fact]
    public void LowStock_UsesStrictlyBelowThreshold()
    {
        var item = new InventoryItem(Guid.NewGuid(), 5);

        item.ChangeStock(5, StockMovementType.BatchReceived, "batch", "B-1");
        Assert.False(item.IsLowStock);

        item.ChangeStock(-1, StockMovementType.SaleDispensed, "sale", "S-1");
        Assert.True(item.IsLowStock);
    }
}
