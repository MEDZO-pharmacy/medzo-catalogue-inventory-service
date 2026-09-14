using Medzo.CatalogueInventory.Application.Catalogue;
using Medzo.CatalogueInventory.Domain.Catalogue;
using Medzo.CatalogueInventory.Domain.Inventory;
using Medzo.CatalogueInventory.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Medzo.CatalogueInventory.IntegrationTests;

public sealed class PublicCatalogueTests
{
    [Fact]
    public async Task BrowsePublic_ReturnsOnlyActiveMedicinesWithAvailableStock()
    {
        await using var fixture = await Fixture.CreateAsync();

        var result = await fixture.Service.BrowsePublicAsync(null, 1, 12, TestContext.Current.CancellationToken);

        var medicine = Assert.Single(result.Items);
        Assert.Equal("Available Medicine", medicine.Name);
        Assert.Equal(9.50m, medicine.UnitPrice);
        Assert.DoesNotContain(result.Items, x => x.Name is "Out Of Stock" or "Inactive Medicine");
    }

    [Fact]
    public async Task BrowsePublic_SearchesNameGenericNameAndManufacturer()
    {
        await using var fixture = await Fixture.CreateAsync();

        Assert.Single((await fixture.Service.BrowsePublicAsync("available", 1, 12, TestContext.Current.CancellationToken)).Items);
        Assert.Single((await fixture.Service.BrowsePublicAsync("generic", 1, 12, TestContext.Current.CancellationToken)).Items);
        Assert.Single((await fixture.Service.BrowsePublicAsync("medzo labs", 1, 12, TestContext.Current.CancellationToken)).Items);
        Assert.Empty((await fixture.Service.BrowsePublicAsync("missing", 1, 12, TestContext.Current.CancellationToken)).Items);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        private Fixture(SqliteConnection connection, CatalogueInventoryDbContext db) { this.connection = connection; Db = db; Service = new CatalogueService(db); }
        public CatalogueInventoryDbContext Db { get; }
        public CatalogueService Service { get; }

        public static async Task<Fixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            var db = new CatalogueInventoryDbContext(new DbContextOptionsBuilder<CatalogueInventoryDbContext>().UseSqlite(connection).Options);
            await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
            Add(db, "Available Medicine", true, 6);
            Add(db, "Out Of Stock", true, 0);
            Add(db, "Inactive Medicine", false, 8);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            db.ChangeTracker.Clear();
            return new Fixture(connection, db);
        }

        private static void Add(CatalogueInventoryDbContext db,string name,bool active,int quantity)
        {
            var medicine=new Medicine(name,"Helpful Generic","Medzo Labs",9.50m,DosageForm.Tablet);
            if(!active)medicine.Deactivate();
            var item=new InventoryItem(medicine.Id,10);
            db.MedicineSet.Add(medicine);db.InventoryItemSet.Add(item);
            if(quantity<=0)return;
            var batch=new StockBatch(item.Id,$"BATCH-{name}",null,DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(6)),quantity);
            db.StockBatches.Add(batch);item.ChangeStock(quantity,StockMovementType.BatchReceived,"TestSetup","PUBLIC",batch.Id);
        }

        public async ValueTask DisposeAsync(){await Db.DisposeAsync();await connection.DisposeAsync();}
    }
}
