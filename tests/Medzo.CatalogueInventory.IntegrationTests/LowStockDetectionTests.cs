using Medzo.CatalogueInventory.Application.Catalogue;
using Medzo.CatalogueInventory.Application.Common;
using Medzo.CatalogueInventory.Application.Inventory;
using Medzo.CatalogueInventory.Domain.Catalogue;
using Medzo.CatalogueInventory.Domain.Inventory;
using Medzo.CatalogueInventory.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Medzo.CatalogueInventory.IntegrationTests;

public sealed class LowStockDetectionTests
{
    [Fact]
    public async Task SaleCrossingThreshold_FlagsMedicineAndQueuesOneDetectionEvent()
    {
        await using var fixture = await Fixture.CreateAsync(12, 10);
        var sale = fixture.Event("SALE-LOW", new ExternalStockLine(fixture.MedicineId, 3, null, null, null));

        Assert.True(await fixture.Inventory.ApplySaleAsync(sale, TestContext.Current.CancellationToken));
        Assert.False(await fixture.Inventory.ApplySaleAsync(sale, TestContext.Current.CancellationToken));

        var result = await fixture.Inventory.GetAsync(null, true, 1, 20, TestContext.Current.CancellationToken);
        var low = Assert.Single(result.Items);
        Assert.True(low.IsLowStock);
        Assert.Equal(9, low.QuantityOnHand);
        Assert.Equal(10, low.ReorderThreshold);
        Assert.Single(await fixture.Db.OutboxMessages.Where(x => x.Type == "inventory.low-stock-detected.v1").ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task PurchaseRecoveringStock_RemovesFlagAndQueuesRecoveryEvent()
    {
        await using var fixture = await Fixture.CreateAsync(5, 10);
        var purchase = fixture.Event("PUR-RECOVER", new ExternalStockLine(fixture.MedicineId, 5, "LOT-RECOVER", null, FutureExpiry()));

        Assert.True(await fixture.Inventory.ApplyPurchaseAsync(purchase, TestContext.Current.CancellationToken));

        Assert.Empty((await fixture.Inventory.GetAsync(null, true, 1, 20, TestContext.Current.CancellationToken)).Items);
        Assert.Single(await fixture.Db.OutboxMessages.Where(x => x.Type == "inventory.stock-recovered.v1").ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task FailedMultiLineSale_RollsBackFlagAndOutboxEvents()
    {
        await using var fixture = await Fixture.CreateAsync(12, 10);
        var sale = fixture.Event("SALE-ROLLBACK-LOW",
            new ExternalStockLine(fixture.MedicineId, 3, null, null, null),
            new ExternalStockLine(Guid.NewGuid(), 1, null, null, null));

        await Assert.ThrowsAsync<NotFoundException>(() => fixture.Inventory.ApplySaleAsync(sale, TestContext.Current.CancellationToken));

        var item = await fixture.Db.InventoryItemSet.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(12, item.QuantityOnHand);
        Assert.False(item.IsLowStock);
        Assert.Empty(await fixture.Db.OutboxMessages.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RaisingReorderThreshold_FlagsMedicineAndQueuesDetectionEvent()
    {
        await using var fixture = await Fixture.CreateAsync(12, 10);
        var current = await fixture.Catalogue.GetAsync(fixture.MedicineId, TestContext.Current.CancellationToken);
        var request = new MedicineRequest(current.Name, current.GenericName, current.Manufacturer, current.UnitPrice, current.DosageForm, current.CategoryId, 15);

        var updated = await fixture.Catalogue.UpdateAsync(current.Id, request, current.Version, TestContext.Current.CancellationToken);

        Assert.True(updated.IsLowStock);
        Assert.Single(await fixture.Db.OutboxMessages.Where(x => x.Type == "inventory.low-stock-detected.v1").ToListAsync(TestContext.Current.CancellationToken));
    }

    private static DateOnly FutureExpiry() => DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(6));

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        private Fixture(SqliteConnection connection, CatalogueInventoryDbContext db, Guid medicineId)
        {
            this.connection = connection;
            Db = db;
            MedicineId = medicineId;
            Inventory = new InventoryService(db);
            Catalogue = new CatalogueService(db);
        }

        public CatalogueInventoryDbContext Db { get; }
        public InventoryService Inventory { get; }
        public CatalogueService Catalogue { get; }
        public Guid MedicineId { get; }
        public ExternalStockEvent Event(string sourceId, params ExternalStockLine[] lines) => new(Guid.NewGuid(), sourceId, DateTime.UtcNow, lines);

        public static async Task<Fixture> CreateAsync(int quantity, int threshold)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            var db = new CatalogueInventoryDbContext(new DbContextOptionsBuilder<CatalogueInventoryDbContext>().UseSqlite(connection).Options);
            await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
            var medicine = new Medicine("Low Stock Test", "Test Generic", "Medzo", 10m, DosageForm.Tablet);
            var item = new InventoryItem(medicine.Id, threshold);
            var batch = new StockBatch(item.Id, "LOW-STOCK-BATCH", null, FutureExpiry(), quantity);
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
