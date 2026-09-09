using Medzo.CatalogueInventory.Application.Common;
using Medzo.CatalogueInventory.Application.Inventory;
using Medzo.CatalogueInventory.Domain.Catalogue;
using Medzo.CatalogueInventory.Domain.Inventory;
using Medzo.CatalogueInventory.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Medzo.CatalogueInventory.IntegrationTests;

public sealed class PurchaseRecordedEventTests
{
    [Fact]
    public async Task ApplyPurchase_CreatesBatchMovementProcessedMarkerAndOutboxAtomically()
    {
        await using var fixture = await Fixture.CreateAsync();
        var message = fixture.Purchase("PUR-100", new ExternalStockLine(fixture.MedicineId, 25, "LOT-100", "Supplier delivery", FutureExpiry()));

        var applied = await fixture.Service.ApplyPurchaseAsync(message, TestContext.Current.CancellationToken);

        Assert.True(applied);
        Assert.Equal(25, (await fixture.Db.InventoryItemSet.SingleAsync(TestContext.Current.CancellationToken)).QuantityOnHand);
        var batch = await fixture.Db.StockBatches.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(25, batch.InitialQuantity);
        Assert.Equal(25, batch.RemainingQuantity);
        Assert.Contains(await fixture.Db.MovementSet.ToListAsync(TestContext.Current.CancellationToken), x => x.Type == StockMovementType.PurchaseReceived && x.SourceId == "PUR-100");
        Assert.Single(await fixture.Db.ProcessedEvents.ToListAsync(TestContext.Current.CancellationToken));
        Assert.Contains(await fixture.Db.OutboxMessages.ToListAsync(TestContext.Current.CancellationToken), x => x.Type == "inventory.stock-changed.v1");
        var audit = await fixture.Service.GetPurchaseReceiptsAsync(1, 20, TestContext.Current.CancellationToken);
        Assert.Contains(audit.Items, x => x.PurchaseReference == "PUR-100" && x.BatchNumber == "LOT-100" && x.QuantityReceived == 25);
    }

    [Fact]
    public async Task ApplyPurchase_WhenLaterLineFails_RollsBackEveryEarlierChange()
    {
        await using var fixture = await Fixture.CreateAsync();
        var message = fixture.Purchase("PUR-ROLLBACK",
            new ExternalStockLine(fixture.MedicineId, 10, "LOT-OK", null, FutureExpiry()),
            new ExternalStockLine(Guid.NewGuid(), 5, "LOT-MISSING", null, FutureExpiry()));

        await Assert.ThrowsAsync<NotFoundException>(() => fixture.Service.ApplyPurchaseAsync(message, TestContext.Current.CancellationToken));

        Assert.Equal(0, (await fixture.Db.InventoryItemSet.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken)).QuantityOnHand);
        Assert.Empty(await fixture.Db.StockBatches.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken));
        Assert.Empty(await fixture.Db.MovementSet.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken));
        Assert.Empty(await fixture.Db.ProcessedEvents.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken));
        Assert.Empty(await fixture.Db.OutboxMessages.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ApplyPurchase_WhenEventIsRetried_DoesNotApplyStockTwice()
    {
        await using var fixture = await Fixture.CreateAsync();
        var message = fixture.Purchase("PUR-RETRY", new ExternalStockLine(fixture.MedicineId, 12, "LOT-RETRY", null, FutureExpiry()));

        Assert.True(await fixture.Service.ApplyPurchaseAsync(message, TestContext.Current.CancellationToken));
        Assert.False(await fixture.Service.ApplyPurchaseAsync(message, TestContext.Current.CancellationToken));

        Assert.Equal(12, (await fixture.Db.InventoryItemSet.SingleAsync(TestContext.Current.CancellationToken)).QuantityOnHand);
        Assert.Single(await fixture.Db.StockBatches.ToListAsync(TestContext.Current.CancellationToken));
        Assert.Single(await fixture.Db.MovementSet.ToListAsync(TestContext.Current.CancellationToken));
        Assert.Single(await fixture.Db.ProcessedEvents.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ApplyPurchase_ForExistingBatch_IncreasesBatchAndInventoryBalances()
    {
        await using var fixture = await Fixture.CreateAsync();
        var expiry = FutureExpiry();

        await fixture.Service.ApplyPurchaseAsync(fixture.Purchase("PUR-ONE", new ExternalStockLine(fixture.MedicineId, 10, "LOT-SHARED", null, expiry)), TestContext.Current.CancellationToken);
        await fixture.Service.ApplyPurchaseAsync(fixture.Purchase("PUR-TWO", new ExternalStockLine(fixture.MedicineId, 7, "lot-shared", null, expiry)), TestContext.Current.CancellationToken);

        Assert.Equal(17, (await fixture.Db.InventoryItemSet.SingleAsync(TestContext.Current.CancellationToken)).QuantityOnHand);
        var batch = await fixture.Db.StockBatches.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(17, batch.InitialQuantity);
        Assert.Equal(17, batch.RemainingQuantity);
        Assert.Equal(2, await fixture.Db.MovementSet.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ApplyPurchase_WithInvalidReference_ReturnsValidationAndChangesNothing()
    {
        await using var fixture = await Fixture.CreateAsync();
        var message = fixture.Purchase(new string('P', StockMovement.MaxSourceIdLength + 1),
            new ExternalStockLine(fixture.MedicineId, 10, "LOT-INVALID", null, FutureExpiry()));

        var error = await Assert.ThrowsAsync<ValidationException>(() =>
            fixture.Service.ApplyPurchaseAsync(message, TestContext.Current.CancellationToken));

        Assert.Contains("sourceId", error.Errors.Keys);
        Assert.Equal(0, (await fixture.Db.InventoryItemSet.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken)).QuantityOnHand);
        Assert.Empty(await fixture.Db.StockBatches.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken));
        Assert.Empty(await fixture.Db.MovementSet.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken));
        Assert.Empty(await fixture.Db.ProcessedEvents.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken));
        Assert.Empty(await fixture.Db.OutboxMessages.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken));
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
            Service = new InventoryService(db);
        }

        public CatalogueInventoryDbContext Db { get; }
        public InventoryService Service { get; }
        public Guid MedicineId { get; }
        public ExternalStockEvent Purchase(string sourceId, params ExternalStockLine[] lines) => new(Guid.NewGuid(), sourceId, DateTime.UtcNow, lines);

        public static async Task<Fixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            var db = new CatalogueInventoryDbContext(new DbContextOptionsBuilder<CatalogueInventoryDbContext>().UseSqlite(connection).Options);
            await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
            var medicine = new Medicine("Amoxicillin", "Amoxicillin", "Medzo", 20m, DosageForm.Capsule);
            db.MedicineSet.Add(medicine);
            db.InventoryItemSet.Add(new InventoryItem(medicine.Id, 5));
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
