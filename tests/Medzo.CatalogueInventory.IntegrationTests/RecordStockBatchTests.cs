using Medzo.CatalogueInventory.Application.Common;
using Medzo.CatalogueInventory.Application.Inventory;
using Medzo.CatalogueInventory.Domain.Catalogue;
using Medzo.CatalogueInventory.Domain.Inventory;
using Medzo.CatalogueInventory.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Medzo.CatalogueInventory.IntegrationTests;

public sealed class RecordStockBatchTests
{
    [Fact]
    public async Task RecordBatch_PersistsTraceabilityAndStockChangesInOneTransaction()
    {
        await using var fixture = await Fixture.CreateAsync();
        var result = await fixture.Service.RecordBatchAsync(new(fixture.MedicineId, "LOT-100", null, DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(6)), 20, "INV-100"), default);

        Assert.Equal("LOT-100", result.BatchNumber);
        Assert.Equal(20, result.QuantityOnHand);
        Assert.Single(await fixture.Db.StockBatches.ToListAsync());
        Assert.Single(await fixture.Db.MovementSet.Where(x => x.SourceId == "INV-100").ToListAsync());
        Assert.Contains(await fixture.Db.OutboxMessages.ToListAsync(), x => x.Type == "inventory.stock-changed.v1");
    }

    [Fact]
    public async Task RecordBatch_WhenBatchNumberAlreadyExists_DoesNotIncreaseStockAgain()
    {
        await using var fixture = await Fixture.CreateAsync();
        var request = new RecordBatchRequest(fixture.MedicineId, "LOT-100", null, DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(6)), 20, "INV-100");
        await fixture.Service.RecordBatchAsync(request, default);

        var error = await Assert.ThrowsAsync<ConflictException>(() => fixture.Service.RecordBatchAsync(request with { BatchNumber = "lot-100" }, default));
        Assert.Contains("Stock was not changed", error.Message);
        Assert.Equal(20, (await fixture.Db.InventoryItemSet.SingleAsync()).QuantityOnHand);
        Assert.Single(await fixture.Db.StockBatches.ToListAsync());
        Assert.Single(await fixture.Db.MovementSet.ToListAsync());
    }

    [Fact]
    public async Task RecordBatch_WhenSourceReferenceIsTooLong_DoesNotWriteAnyData()
    {
        await using var fixture = await Fixture.CreateAsync();
        var request = new RecordBatchRequest(fixture.MedicineId, "LOT-TOO-LONG", null, DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(6)), 20, new string('x', 151));

        var error = await Assert.ThrowsAsync<ValidationException>(() => fixture.Service.RecordBatchAsync(request, TestContext.Current.CancellationToken));

        Assert.Contains("sourceReference", error.Errors.Keys);
        Assert.Equal(0, (await fixture.Db.InventoryItemSet.SingleAsync(TestContext.Current.CancellationToken)).QuantityOnHand);
        Assert.Empty(await fixture.Db.StockBatches.ToListAsync(TestContext.Current.CancellationToken));
        Assert.Empty(await fixture.Db.MovementSet.ToListAsync(TestContext.Current.CancellationToken));
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        private Fixture(SqliteConnection connection, CatalogueInventoryDbContext db, Guid medicineId) { this.connection = connection; Db = db; MedicineId = medicineId; Service = new InventoryService(db); }
        public CatalogueInventoryDbContext Db { get; }
        public InventoryService Service { get; }
        public Guid MedicineId { get; }

        public static async Task<Fixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
            var db = new CatalogueInventoryDbContext(new DbContextOptionsBuilder<CatalogueInventoryDbContext>().UseSqlite(connection).Options);
            await db.Database.EnsureCreatedAsync();
            var medicine = new Medicine("Paracetamol", "Paracetamol", "Medzo", 10m, DosageForm.Tablet);
            db.MedicineSet.Add(medicine); db.InventoryItemSet.Add(new InventoryItem(medicine.Id, 5)); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
            Assert.Equal(1, (await db.InventoryItemSet.AsNoTracking().SingleAsync()).Version);
            return new Fixture(connection, db, medicine.Id);
        }

        public async ValueTask DisposeAsync() { await Db.DisposeAsync(); await connection.DisposeAsync(); }
    }
}
