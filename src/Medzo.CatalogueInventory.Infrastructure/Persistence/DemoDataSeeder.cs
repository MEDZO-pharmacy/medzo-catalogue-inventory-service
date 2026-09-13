using Medzo.CatalogueInventory.Domain.Catalogue;
using Medzo.CatalogueInventory.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Medzo.CatalogueInventory.Infrastructure.Persistence;

public static class DemoDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<CatalogueInventoryDbContext>();

        if (database.Database.IsSqlite()) await database.Database.EnsureCreatedAsync(cancellationToken);
        else await database.Database.MigrateAsync(cancellationToken);

        await AddMedicineAsync(
            database,
            "Paracetamol 500mg",
            "Paracetamol",
            "Medzo Labs",
            12.50m,
            DosageForm.Tablet,
            reorderThreshold: 30,
            batchNumber: "DEMO-PARA-001",
            expiryDate: DateOnly.FromDateTime(DateTime.UtcNow.AddYears(2)),
            initialQuantity: 120,
            purchaseQuantity: 40,
            saleQuantity: 25,
            cancellationToken);

        await AddMedicineAsync(
            database,
            "Amoxicillin 250mg",
            "Amoxicillin",
            "Ceylon Pharmaceuticals",
            35.00m,
            DosageForm.Capsule,
            reorderThreshold: 20,
            batchNumber: "DEMO-AMOX-001",
            expiryDate: DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(14)),
            initialQuantity: 8,
            purchaseQuantity: 0,
            saleQuantity: 0,
            cancellationToken);

        await AddMedicineAsync(
            database,
            "Cetirizine 10mg",
            "Cetirizine Hydrochloride",
            "HealthCare Pharma",
            18.75m,
            DosageForm.Tablet,
            reorderThreshold: 10,
            batchNumber: "DEMO-CET-001",
            expiryDate: DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(20)),
            initialQuantity: 60,
            purchaseQuantity: 0,
            saleQuantity: 12,
            cancellationToken);
    }

    private static async Task AddMedicineAsync(
        CatalogueInventoryDbContext database,
        string name,
        string genericName,
        string manufacturer,
        decimal unitPrice,
        DosageForm dosageForm,
        int reorderThreshold,
        string batchNumber,
        DateOnly expiryDate,
        int initialQuantity,
        int purchaseQuantity,
        int saleQuantity,
        CancellationToken cancellationToken)
    {
        var normalizedName = name.ToUpperInvariant();
        if (await database.MedicineSet.AnyAsync(x => x.NormalizedName == normalizedName, cancellationToken))
        {
            return;
        }

        var medicine = new Medicine(name, genericName, manufacturer, unitPrice, dosageForm);
        var inventory = new InventoryItem(medicine.Id, reorderThreshold);
        var batch = new StockBatch(
            inventory.Id,
            batchNumber,
            "Development demonstration stock",
            expiryDate,
            initialQuantity);

        database.MedicineSet.Add(medicine);
        database.InventoryItemSet.Add(inventory);
        database.StockBatches.Add(batch);
        inventory.ChangeStock(initialQuantity, StockMovementType.BatchReceived, "DemoSeed", batchNumber, batch.Id);

        if (purchaseQuantity > 0)
        {
            batch.Receive(purchaseQuantity);
            inventory.ChangeStock(purchaseQuantity, StockMovementType.PurchaseReceived, "Purchase", $"DEMO-PO-{batchNumber}", batch.Id);
        }

        if (saleQuantity > 0)
        {
            batch.Consume(saleQuantity);
            inventory.ChangeStock(-saleQuantity, StockMovementType.SaleDispensed, "Sale", $"DEMO-SALE-{batchNumber}", batch.Id);
        }

        await database.SaveChangesAsync(cancellationToken);
    }
}
