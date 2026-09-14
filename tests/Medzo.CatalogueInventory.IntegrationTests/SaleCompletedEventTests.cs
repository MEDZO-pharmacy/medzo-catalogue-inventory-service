using Medzo.CatalogueInventory.Application.Common;
using Medzo.CatalogueInventory.Application.Inventory;
using Medzo.CatalogueInventory.Domain.Catalogue;
using Medzo.CatalogueInventory.Domain.Inventory;
using Medzo.CatalogueInventory.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Medzo.CatalogueInventory.IntegrationTests;

public sealed class SaleCompletedEventTests
{
    [Fact]
    public async Task ApplySale_DecreasesBatchAndInventoryAndCreatesAuditRecords()
    {
        await using var fixture = await Fixture.CreateAsync(30);
        var message = fixture.Sale("SALE-100", new ExternalStockLine(fixture.MedicineId, 12, null, null, null));

        var applied = await fixture.Service.ApplySaleAsync(message, TestContext.Current.CancellationToken);

        Assert.True(applied);
        Assert.Equal(18, (await fixture.Db.InventoryItemSet.SingleAsync(TestContext.Current.CancellationToken)).QuantityOnHand);
        Assert.Equal(18, (await fixture.Db.StockBatches.SingleAsync(TestContext.Current.CancellationToken)).RemainingQuantity);
        Assert.Contains(await fixture.Db.MovementSet.ToListAsync(TestContext.Current.CancellationToken), x => x.Type == StockMovementType.SaleDispensed && x.QuantityDelta == -12 && x.SourceId == "SALE-100");
        Assert.Single(await fixture.Db.ProcessedEvents.ToListAsync(TestContext.Current.CancellationToken));
        Assert.Contains(await fixture.Db.OutboxMessages.ToListAsync(TestContext.Current.CancellationToken), x => x.Type == "inventory.stock-changed.v1");
        var audit = await fixture.Service.GetSaleIssuesAsync(1, 20, TestContext.Current.CancellationToken);
        Assert.Contains(audit.Items, x => x.SaleReference == "SALE-100" && x.QuantitySold == 12 && x.QuantityAfter == 18);
    }

    [Fact]
    public async Task ApplySale_WhenLaterLineFails_RollsBackEveryEarlierChange()
    {
        await using var fixture = await Fixture.CreateAsync(30);
        var originalMovements = await fixture.Db.MovementSet.CountAsync(TestContext.Current.CancellationToken);
        var message = fixture.Sale("SALE-ROLLBACK",
            new ExternalStockLine(fixture.MedicineId, 10, null, null, null),
            new ExternalStockLine(Guid.NewGuid(), 2, null, null, null));

        await Assert.ThrowsAsync<NotFoundException>(() => fixture.Service.ApplySaleAsync(message, TestContext.Current.CancellationToken));

        Assert.Equal(30, (await fixture.Db.InventoryItemSet.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken)).QuantityOnHand);
        Assert.Equal(30, (await fixture.Db.StockBatches.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken)).RemainingQuantity);
        Assert.Equal(originalMovements, await fixture.Db.MovementSet.AsNoTracking().CountAsync(TestContext.Current.CancellationToken));
        Assert.Empty(await fixture.Db.ProcessedEvents.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken));
        Assert.Empty(await fixture.Db.OutboxMessages.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ApplySale_WhenEventIsRetried_DoesNotDecreaseStockTwice()
    {
        await using var fixture = await Fixture.CreateAsync(30);
        var message = fixture.Sale("SALE-RETRY", new ExternalStockLine(fixture.MedicineId, 8, null, null, null));

        Assert.True(await fixture.Service.ApplySaleAsync(message, TestContext.Current.CancellationToken));
        Assert.False(await fixture.Service.ApplySaleAsync(message, TestContext.Current.CancellationToken));

        Assert.Equal(22, (await fixture.Db.InventoryItemSet.SingleAsync(TestContext.Current.CancellationToken)).QuantityOnHand);
        Assert.Equal(22, (await fixture.Db.StockBatches.SingleAsync(TestContext.Current.CancellationToken)).RemainingQuantity);
        Assert.Single(await fixture.Db.MovementSet.Where(x => x.Type == StockMovementType.SaleDispensed).ToListAsync(TestContext.Current.CancellationToken));
        Assert.Single(await fixture.Db.ProcessedEvents.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ApplySale_WithInsufficientSellableStock_RollsBackAndReturnsConflict()
    {
        await using var fixture = await Fixture.CreateAsync(5);
        var message = fixture.Sale("SALE-TOO-LARGE", new ExternalStockLine(fixture.MedicineId, 6, null, null, null));

        var error = await Assert.ThrowsAsync<ConflictException>(() => fixture.Service.ApplySaleAsync(message, TestContext.Current.CancellationToken));

        Assert.Equal("Insufficient sellable stock.", error.Message);
        Assert.Equal(5, (await fixture.Db.InventoryItemSet.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken)).QuantityOnHand);
        Assert.Equal(5, (await fixture.Db.StockBatches.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken)).RemainingQuantity);
        Assert.Empty(await fixture.Db.ProcessedEvents.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken));
        Assert.Empty(await fixture.Db.OutboxMessages.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken));
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        private Fixture(SqliteConnection connection, CatalogueInventoryDbContext db, Guid medicineId)
        {
            this.connection = connection;
            Db = db;
            MedicineId = medicineId;
            Service = new InventoryService(db);
        }

        public CatalogueInventoryDbContext Db { get; }
        public InventoryService Service { get; }
        public Guid MedicineId { get; }
        public ExternalStockEvent Sale(string sourceId, params ExternalStockLine[] lines) => new(Guid.NewGuid(), sourceId, DateTime.UtcNow, lines);

        public static async Task<Fixture> CreateAsync(int quantity)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            var db = new CatalogueInventoryDbContext(new DbContextOptionsBuilder<CatalogueInventoryDbContext>().UseSqlite(connection).Options);
            await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
            var medicine = new Medicine("Paracetamol", "Acetaminophen", "Medzo", 10m, DosageForm.Tablet);
            var item = new InventoryItem(medicine.Id, 10);
            var batch = new StockBatch(item.Id, "SALE-BATCH", null, DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(6)), quantity);
            db.MedicineSet.Add(medicine);
            db.InventoryItemSet.Add(item);
            db.StockBatches.Add(batch);
            item.ChangeStock(quantity, StockMovementType.BatchReceived, "TestSetup", "SETUP", batch.Id);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            db.ChangeTracker.Clear();
            return new Fixture(connection, db, medicine.Id);
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
