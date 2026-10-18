using Medzo.CatalogueInventory.Domain.Inventory;
using Xunit;

namespace Medzo.CatalogueInventory.UnitTests;

public sealed class StockBatchTests
{
    private static readonly DateOnly FutureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));

    [Fact]
    public void Constructor_StoresTrimmedTraceabilityDetails()
    {
        var inventoryItemId = Guid.NewGuid();
        var before = DateTime.UtcNow;

        var batch = new StockBatch(
            inventoryItemId, " lot-001 ", " Supplier delivery INV-100 ", FutureDate, 25);

        var after = DateTime.UtcNow;
        Assert.Equal(inventoryItemId, batch.InventoryItemId);
        Assert.Equal("lot-001", batch.BatchNumber);
        Assert.Equal("LOT-001", batch.NormalizedBatchNumber);
        Assert.Equal("Supplier delivery INV-100", batch.Description);
        Assert.Equal(FutureDate, batch.ExpiryDate);
        Assert.Equal(25, batch.InitialQuantity);
        Assert.Equal(25, batch.RemainingQuantity);
        Assert.InRange(batch.ReceivedAtUtc, before, after);
    }

    [Fact]
    public void Constructor_WithMissingInventoryItem_ThrowsClearError()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new StockBatch(Guid.Empty, "LOT-001", null, FutureDate, 1));

        Assert.Equal("inventoryItemId", exception.ParamName);
        Assert.StartsWith("Inventory item is required.", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithMissingBatchNumber_ThrowsClearError(string? batchNumber)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new StockBatch(Guid.NewGuid(), batchNumber!, null, FutureDate, 1));

        Assert.Equal("batchNumber", exception.ParamName);
        Assert.StartsWith("Batch number is required.", exception.Message);
    }

    [Fact]
    public void Constructor_AcceptsBatchNumberAtTrimmedMaximumLength()
    {
        var value = $" {new string('x', StockBatch.MaxBatchNumberLength)} ";

        var batch = new StockBatch(Guid.NewGuid(), value, null, FutureDate, 1);

        Assert.Equal(StockBatch.MaxBatchNumberLength, batch.BatchNumber.Length);
    }

    [Fact]
    public void Constructor_RejectsBatchNumberBeyondMaximumLength()
    {
        var value = new string('x', StockBatch.MaxBatchNumberLength + 1);

        var exception = Assert.Throws<ArgumentException>(() =>
            new StockBatch(Guid.NewGuid(), value, null, FutureDate, 1));

        Assert.Equal("batchNumber", exception.ParamName);
        Assert.StartsWith("Batch number cannot exceed 100 characters.", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_AllowsMissingOptionalDescription(string? description)
    {
        var batch = new StockBatch(Guid.NewGuid(), "LOT-001", description, FutureDate, 1);

        Assert.Null(batch.Description);
    }

    [Fact]
    public void Constructor_EnforcesTrimmedDescriptionBoundary()
    {
        var maximum = $" {new string('x', StockBatch.MaxDescriptionLength)} ";
        var tooLong = new string('x', StockBatch.MaxDescriptionLength + 1);

        var batch = new StockBatch(Guid.NewGuid(), "LOT-001", maximum, FutureDate, 1);
        var exception = Assert.Throws<ArgumentException>(() =>
            new StockBatch(Guid.NewGuid(), "LOT-002", tooLong, FutureDate, 1));

        Assert.Equal(StockBatch.MaxDescriptionLength, batch.Description!.Length);
        Assert.Equal("description", exception.ParamName);
        Assert.StartsWith("Description cannot exceed 500 characters.", exception.Message);
    }

    [Fact]
    public void Constructor_RejectsExpiryDateOfToday()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var exception = Assert.Throws<ArgumentException>(() =>
            new StockBatch(Guid.NewGuid(), "LOT-001", null, today, 1));

        Assert.Equal("expiryDate", exception.ParamName);
        Assert.StartsWith("Expiry date must be in the future.", exception.Message);
    }

    [Fact]
    public void Constructor_AcceptsExpiryDateOfTomorrow()
    {
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

        var batch = new StockBatch(Guid.NewGuid(), "LOT-001", null, tomorrow, 1);

        Assert.Equal(tomorrow, batch.ExpiryDate);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_RejectsNonPositiveInitialQuantity(int quantity)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new StockBatch(Guid.NewGuid(), "LOT-001", null, FutureDate, quantity));

        Assert.Equal("initialQuantity", exception.ParamName);
        Assert.StartsWith("Quantity must be greater than zero.", exception.Message);
    }

    [Fact]
    public void Receive_IncreasesInitialAndRemainingQuantities()
    {
        var batch = ValidBatch(10);

        batch.Receive(5);

        Assert.Equal(15, batch.InitialQuantity);
        Assert.Equal(15, batch.RemainingQuantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Receive_RejectsNonPositiveQuantities(int quantity)
    {
        var batch = ValidBatch(10);

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => batch.Receive(quantity));

        Assert.Equal("quantity", exception.ParamName);
        Assert.StartsWith("Quantity must be greater than zero.", exception.Message);
    }

    [Fact]
    public void Receive_ThrowsWhenInitialQuantityOverflows()
    {
        var batch = ValidBatch(int.MaxValue);
        batch.Consume(1);

        Assert.Throws<OverflowException>(() => batch.Receive(1));
    }

    [Fact]
    public void Consume_DecreasesOnlyRemainingQuantity()
    {
        var batch = ValidBatch(10);

        batch.Consume(4);

        Assert.Equal(10, batch.InitialQuantity);
        Assert.Equal(6, batch.RemainingQuantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(11)]
    public void Consume_RejectsInvalidQuantities(int quantity)
    {
        var batch = ValidBatch(10);

        var exception = Assert.Throws<InvalidOperationException>(() => batch.Consume(quantity));

        Assert.Equal("Invalid batch quantity.", exception.Message);
        Assert.Equal(10, batch.RemainingQuantity);
    }

    [Fact]
    public void Consume_AllowsExactlyTheRemainingQuantity()
    {
        var batch = ValidBatch(10);

        batch.Consume(10);

        Assert.Equal(0, batch.RemainingQuantity);
    }

    private static StockBatch ValidBatch(int initialQuantity) =>
        new(Guid.NewGuid(), "LOT-001", null, FutureDate, initialQuantity);
}
