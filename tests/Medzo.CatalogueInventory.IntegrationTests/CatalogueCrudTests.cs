using Medzo.CatalogueInventory.Application.Catalogue;
using Medzo.CatalogueInventory.Application.Common;
using Medzo.CatalogueInventory.Domain.Catalogue;
using Medzo.CatalogueInventory.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Medzo.CatalogueInventory.IntegrationTests;

public sealed class CatalogueCrudTests
{
    [Fact]
    public async Task Create_PersistsMedicineAndItsInventoryItemAtomically()
    {
        await using var fixture = await Fixture.CreateAsync();

        var created = await fixture.Service.CreateAsync(Request("Paracetamol"), TestContext.Current.CancellationToken);

        Assert.Equal("Paracetamol", created.Name);
        Assert.Equal(5, created.ReorderThreshold);
        Assert.Equal(0, created.QuantityOnHand);
        Assert.True(await fixture.Db.MedicineSet.AnyAsync(x => x.Id == created.Id, TestContext.Current.CancellationToken));
        Assert.True(await fixture.Db.InventoryItemSet.AnyAsync(x => x.MedicineId == created.Id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Get_ReturnsThePersistedMedicine()
    {
        await using var fixture = await Fixture.CreateAsync();
        var created = await fixture.Service.CreateAsync(Request("Amoxicillin"), TestContext.Current.CancellationToken);
        fixture.Db.ChangeTracker.Clear();

        var result = await fixture.Service.GetAsync(created.Id, TestContext.Current.CancellationToken);

        Assert.Equal(created.Id, result.Id);
        Assert.Equal("Amoxicillin", result.Name);
        Assert.Equal(DosageForm.Tablet, result.DosageForm);
    }

    [Fact]
    public async Task Update_ChangesMedicineWithoutChangingStockQuantity()
    {
        await using var fixture = await Fixture.CreateAsync();
        var created = await fixture.Service.CreateAsync(Request("Old Name"), TestContext.Current.CancellationToken);

        var updated = await fixture.Service.UpdateAsync(
            created.Id,
            Request("New Name") with { UnitPrice = 15.75m, ReorderThreshold = 8 },
            created.Version,
            TestContext.Current.CancellationToken);

        Assert.Equal("New Name", updated.Name);
        Assert.Equal(15.75m, updated.UnitPrice);
        Assert.Equal(8, updated.ReorderThreshold);
        Assert.Equal(0, updated.QuantityOnHand);
    }

    [Fact]
    public async Task Create_WhenNameAlreadyExists_RollsBackWithoutCreatingAnotherInventoryItem()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.Service.CreateAsync(Request("Paracetamol"), TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<ConflictException>(() =>
            fixture.Service.CreateAsync(Request("  PARACETAMOL  "), TestContext.Current.CancellationToken));

        Assert.Equal(1, await fixture.Db.MedicineSet.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(1, await fixture.Db.InventoryItemSet.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Create_WhenTextExceedsDatabaseLimit_ReturnsValidationWithoutWritingData()
    {
        await using var fixture = await Fixture.CreateAsync();

        var error = await Assert.ThrowsAsync<ValidationException>(() =>
            fixture.Service.CreateAsync(Request(new string('x', Medicine.MaxNameLength + 1)), TestContext.Current.CancellationToken));

        Assert.Contains("name", error.Errors.Keys);
        Assert.Empty(await fixture.Db.MedicineSet.ToListAsync(TestContext.Current.CancellationToken));
        Assert.Empty(await fixture.Db.InventoryItemSet.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Update_WithStaleVersion_DoesNotOverwriteMedicine()
    {
        await using var fixture = await Fixture.CreateAsync();
        var created = await fixture.Service.CreateAsync(Request("Original"), TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<ConflictException>(() => fixture.Service.UpdateAsync(
            created.Id, Request("Overwritten"), created.Version + 1, TestContext.Current.CancellationToken));

        var unchanged = await fixture.Service.GetAsync(created.Id, TestContext.Current.CancellationToken);
        Assert.Equal("Original", unchanged.Name);
    }

    private static MedicineRequest Request(string name) =>
        new(name, "Generic", "Medzo Labs", 10m, DosageForm.Tablet, null, 5);

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;

        private Fixture(SqliteConnection connection, CatalogueInventoryDbContext db)
        {
            this.connection = connection;
            Db = db;
            Service = new CatalogueService(db);
        }

        public CatalogueInventoryDbContext Db { get; }
        public CatalogueService Service { get; }

        public static async Task<Fixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var db = new CatalogueInventoryDbContext(new DbContextOptionsBuilder<CatalogueInventoryDbContext>()
                .UseSqlite(connection).Options);
            await db.Database.EnsureCreatedAsync();
            return new Fixture(connection, db);
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
