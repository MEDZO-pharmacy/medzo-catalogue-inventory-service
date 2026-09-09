using Medzo.CatalogueInventory.Domain.Common;

namespace Medzo.CatalogueInventory.Domain.Inventory;

public sealed class StockBatch : Entity
{
    public const int MaxBatchNumberLength = 100;
    public const int MaxDescriptionLength = 500;
    private StockBatch() { }

    public StockBatch(Guid inventoryItemId, string batchNumber, string? description, DateOnly expiryDate, int initialQuantity)
    {
        if (inventoryItemId == Guid.Empty) throw new ArgumentException("Inventory item is required.", nameof(inventoryItemId));
        if (string.IsNullOrWhiteSpace(batchNumber)) throw new ArgumentException("Batch number is required.", nameof(batchNumber));
        if (batchNumber.Trim().Length > MaxBatchNumberLength) throw new ArgumentException($"Batch number cannot exceed {MaxBatchNumberLength} characters.", nameof(batchNumber));
        if ((description?.Trim().Length ?? 0) > MaxDescriptionLength) throw new ArgumentException($"Description cannot exceed {MaxDescriptionLength} characters.", nameof(description));
        if (expiryDate <= DateOnly.FromDateTime(DateTime.UtcNow)) throw new ArgumentException("Expiry date must be in the future.", nameof(expiryDate));
        if (initialQuantity <= 0) throw new ArgumentOutOfRangeException(nameof(initialQuantity), "Quantity must be greater than zero.");
        InventoryItemId = inventoryItemId; BatchNumber = batchNumber.Trim(); NormalizedBatchNumber = BatchNumber.ToUpperInvariant(); Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(); ExpiryDate = expiryDate;
        InitialQuantity = RemainingQuantity = initialQuantity; ReceivedAtUtc = DateTime.UtcNow;
    }

    public Guid InventoryItemId { get; private set; }
    public string BatchNumber { get; private set; } = null!;
    public string NormalizedBatchNumber { get; private set; } = null!;
    public string? Description { get; private set; }
    public DateOnly ExpiryDate { get; private set; }
    public int InitialQuantity { get; private set; }
    public int RemainingQuantity { get; private set; }
    public DateTime ReceivedAtUtc { get; private set; }
    public void Receive(int quantity)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        InitialQuantity = checked(InitialQuantity + quantity);
        RemainingQuantity = checked(RemainingQuantity + quantity);
    }
    public void Consume(int quantity) { if (quantity <= 0 || quantity > RemainingQuantity) throw new InvalidOperationException("Invalid batch quantity."); RemainingQuantity -= quantity; }
}
