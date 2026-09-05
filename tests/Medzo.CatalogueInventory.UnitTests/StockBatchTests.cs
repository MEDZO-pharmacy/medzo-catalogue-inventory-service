using Medzo.CatalogueInventory.Domain.Inventory;
using Xunit;

namespace Medzo.CatalogueInventory.UnitTests;

public sealed class StockBatchTests
{
    private static readonly DateOnly FutureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));

    [Fact]
    public void Constructor_StoresTrimmedTraceabilityDetails()
    {
        var batch = new StockBatch(Guid.NewGuid(), " LOT-001 ", " Supplier delivery INV-100 ", FutureDate, 25);
        Assert.Equal("LOT-001", batch.BatchNumber);
        Assert.Equal("Supplier delivery INV-100", batch.Description);
        Assert.Equal(FutureDate, batch.ExpiryDate);
        Assert.Equal(25, batch.InitialQuantity);
        Assert.Equal(25, batch.RemainingQuantity);
    }

    [Fact]
    public void Constructor_RejectsMissingBatchNumber() =>
        Assert.Throws<ArgumentException>(() => new StockBatch(Guid.NewGuid(), "", null!, FutureDate, 1));

    [Fact]
    public void Constructor_AllowsOptionalDescription() =>
        Assert.Null(new StockBatch(Guid.NewGuid(), "LOT-001", null!, FutureDate, 1).Description);

    [Fact]
    public void Constructor_RejectsExpiredBatch() =>
        Assert.Throws<ArgumentException>(() => new StockBatch(Guid.NewGuid(), "LOT-001", "Source", DateOnly.FromDateTime(DateTime.UtcNow), 1));
}
