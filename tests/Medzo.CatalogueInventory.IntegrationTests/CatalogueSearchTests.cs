using Medzo.CatalogueInventory.Application.Catalogue;
using Medzo.CatalogueInventory.Domain.Catalogue;
using Medzo.CatalogueInventory.Domain.Inventory;
using Medzo.CatalogueInventory.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Medzo.CatalogueInventory.IntegrationTests;

public sealed class CatalogueSearchTests
{
    [Fact]
    public async Task Search_WhenNameContainsTerm_ReturnsMatchingActiveMedicinesCaseInsensitively()
    {
        await using var fixture = await Fixture.CreateAsync();

        var result = await fixture.Service.SearchAsync("  CETA  ", 1, 20, TestContext.Current.CancellationToken);

        var medicine = Assert.Single(result.Items);
        Assert.Equal("Paracetamol", medicine.Name);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task Search_WhenNothingMatches_ReturnsAnEmptyPage()
    {
        await using var fixture = await Fixture.CreateAsync();

        var result = await fixture.Service.SearchAsync("does-not-exist", 1, 20, TestContext.Current.CancellationToken);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Search_WhenTermIsCleared_RestoresFullActiveCatalogue(string? search)
    {
        await using var fixture = await Fixture.CreateAsync();

        var result = await fixture.Service.SearchAsync(search, 1, 20, TestContext.Current.CancellationToken);

        Assert.Equal(2, result.TotalCount);
        Assert.Collection(
            result.Items,
            item => Assert.Equal("Amoxicillin", item.Name),
            item => Assert.Equal("Paracetamol", item.Name));
    }

    [Fact]
    public async Task Search_DoesNotReturnInactiveMedicines()
    {
        await using var fixture = await Fixture.CreateAsync();

        var result = await fixture.Service.SearchAsync("retired", 1, 20, TestContext.Current.CancellationToken);

        Assert.Empty(result.Items);
    }

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
            var options = new DbContextOptionsBuilder<CatalogueInventoryDbContext>()
                .UseSqlite(connection)
                .Options;
            var db = new CatalogueInventoryDbContext(options);
            await db.Database.EnsureCreatedAsync();

            var paracetamol = new Medicine("Paracetamol", "Acetaminophen", "Medzo Labs", 12.50m, DosageForm.Tablet);
            var amoxicillin = new Medicine("Amoxicillin", "Amoxicillin", "Health Pharma", 25m, DosageForm.Capsule);
            var inactive = new Medicine("Retired Medicine", "Old Generic", "Old Pharma", 5m, DosageForm.Tablet);
            inactive.Deactivate();

            db.MedicineSet.AddRange(paracetamol, amoxicillin, inactive);
            db.InventoryItemSet.AddRange(
                new InventoryItem(paracetamol.Id, 10),
                new InventoryItem(amoxicillin.Id, 5),
                new InventoryItem(inactive.Id, 1));
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            return new Fixture(connection, db);
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
