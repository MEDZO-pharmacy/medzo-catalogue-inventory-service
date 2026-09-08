using Medzo.CatalogueInventory.Domain.Inventory;
using Xunit;

namespace Medzo.CatalogueInventory.UnitTests;

public sealed class StockBatchReceiptTests
{
    [Fact]
    public void Receive_IncreasesInitialAndRemainingQuantities()
    {
        var batch = new StockBatch(Guid.NewGuid(), "LOT-10", null, DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3)), 10);

        batch.Receive(5);

        Assert.Equal(15, batch.InitialQuantity);
        Assert.Equal(15, batch.RemainingQuantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Receive_RejectsNonPositiveQuantities(int quantity)
    {
        var batch = new StockBatch(Guid.NewGuid(), "LOT-10", null, DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3)), 10);

        Assert.Throws<ArgumentOutOfRangeException>(() => batch.Receive(quantity));
    }
}
