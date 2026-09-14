using Medzo.CatalogueInventory.Application.Common;
using Medzo.CatalogueInventory.Domain.Inventory;
using Microsoft.EntityFrameworkCore;

namespace Medzo.CatalogueInventory.Application.Inventory;

public sealed class InventoryService(ICatalogueInventoryStore store) : IInventoryService
{
    public async Task<PagedResult<InventoryResponse>> GetAsync(string? search, bool low, int page, int size, CancellationToken ct)
    {
        (page, size) = Page(page, size);
        var query = store.InventoryItems.AsNoTracking().Include(x => x.Batches)
            .Join(store.Medicines.Where(m => m.IsActive), i => i.MedicineId, m => m.Id, (i, m) => new { i, m });
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x => x.m.Name.Contains(search) || x.m.GenericName.Contains(search));
        if (low) query = query.Where(x => x.i.QuantityOnHand < x.i.ReorderThreshold);
        var total = await query.CountAsync(ct);
        var rows = await query.OrderBy(x => x.m.Name).Skip((page - 1) * size).Take(size).ToListAsync(ct);
        return new(rows.Select(x => MapInventory(x.i, x.m.Name, x.m.GenericName)).ToList(), page, size, total);
    }

    public async Task<PagedResult<BatchResponse>> GetBatchesAsync(Guid? medicineId, int page, int size, CancellationToken ct)
    {
        (page, size) = Page(page, size);
        var query = store.StockBatches.AsNoTracking()
            .Join(store.InventoryItems, b => b.InventoryItemId, i => i.Id, (b, i) => new { b, i })
            .Join(store.Medicines, x => x.i.MedicineId, m => m.Id, (x, m) => new { x.b, x.i, m });
        if (medicineId.HasValue) query = query.Where(x => x.m.Id == medicineId.Value);
        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(x => x.b.ReceivedAtUtc).Skip((page - 1) * size).Take(size).ToListAsync(ct);
        return new(rows.Select(x => MapBatch(x.b, x.i, x.m.Name)).ToList(), page, size, total);
    }

    public async Task<BatchResponse> GetBatchAsync(Guid id, CancellationToken ct)
    {
        var row = await store.StockBatches.AsNoTracking().Where(b => b.Id == id)
            .Join(store.InventoryItems, b => b.InventoryItemId, i => i.Id, (b, i) => new { b, i })
            .Join(store.Medicines, x => x.i.MedicineId, m => m.Id, (x, m) => new { x.b, x.i, m })
            .SingleOrDefaultAsync(ct) ?? throw new NotFoundException("Stock batch was not found.");
        return MapBatch(row.b, row.i, row.m.Name);
    }

    public async Task<BatchResponse> RecordBatchAsync(RecordBatchRequest request, CancellationToken ct)
    {
        ValidateBatch(request);
        var normalizedBatch = request.BatchNumber.Trim();
        var normalizedKey = normalizedBatch.ToUpperInvariant();
        try
        {
            return await store.ExecuteTransactionAsync<BatchResponse>(async token =>
            {
                var item = await store.InventoryItems.Include(x => x.Batches).SingleOrDefaultAsync(x => x.MedicineId == request.MedicineId, token)
                    ?? throw new NotFoundException("Inventory item was not found.");
                var medicineName = await store.Medicines.Where(x => x.Id == request.MedicineId).Select(x => x.Name).SingleAsync(token);
                if (item.Batches.Any(x => x.NormalizedBatchNumber == normalizedKey)) throw DuplicateBatch();

                var wasLow = item.IsLowStock;
                var batch = new StockBatch(item.Id, normalizedBatch, request.Description, request.ExpiryDate, request.Quantity);
                await store.AddBatchAsync(batch, token);
                item.ChangeStock(request.Quantity, StockMovementType.BatchReceived, "ManualBatch", request.SourceReference?.Trim() ?? batch.Id.ToString(), batch.Id);
                QueueTransitions(item, wasLow, "ManualBatch", batch.Id.ToString());
                await store.SaveChangesAsync(token);
                return MapBatch(batch, item, medicineName);
            }, ct);
        }
        catch (DbUpdateException)
        {
            var duplicateExists = await store.StockBatches
                .Join(store.InventoryItems, batch => batch.InventoryItemId, item => item.Id, (batch, item) => new { batch, item })
                .AnyAsync(x => x.item.MedicineId == request.MedicineId && x.batch.NormalizedBatchNumber == normalizedKey, ct);
            if (duplicateExists) throw DuplicateBatch();
            throw;
        }
    }

    public async Task<PagedResult<MovementResponse>> GetMovementsAsync(Guid medicineId, int page, int size, CancellationToken ct)
    {
        (page, size) = Page(page, size);
        var item = await store.InventoryItems.AsNoTracking().SingleOrDefaultAsync(x => x.MedicineId == medicineId, ct) ?? throw new NotFoundException("Inventory item was not found.");
        var query = store.StockMovements.AsNoTracking().Where(x => x.InventoryItemId == item.Id);
        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(x => x.OccurredAtUtc).Skip((page - 1) * size).Take(size).ToListAsync(ct);
        return new(rows.Select(x => new MovementResponse(x.Id, x.Type.ToString(), x.QuantityDelta, x.QuantityBefore, x.QuantityAfter, x.SourceType, x.SourceId, null, x.OccurredAtUtc)).ToList(), page, size, total);
    }

    public async Task<PagedResult<PurchaseStockReceiptResponse>> GetPurchaseReceiptsAsync(int page, int size, CancellationToken ct)
    {
        (page, size) = Page(page, size);
        var query = store.StockMovements.AsNoTracking()
            .Where(movement => movement.Type == StockMovementType.PurchaseReceived)
            .Join(store.InventoryItems, movement => movement.InventoryItemId, item => item.Id, (movement, item) => new { movement, item })
            .Join(store.Medicines, row => row.item.MedicineId, medicine => medicine.Id, (row, medicine) => new { row.movement, row.item, medicine })
            .Join(store.StockBatches, row => row.movement.StockBatchId, batch => batch.Id, (row, batch) => new { row.movement, row.item, row.medicine, batch });
        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(x => x.movement.OccurredAtUtc).Skip((page - 1) * size).Take(size).ToListAsync(ct);
        return new(rows.Select(x => new PurchaseStockReceiptResponse(x.movement.Id, x.item.MedicineId, x.medicine.Name, x.batch.BatchNumber, x.batch.ExpiryDate, x.movement.QuantityDelta, x.movement.QuantityBefore, x.movement.QuantityAfter, x.movement.SourceId, x.movement.OccurredAtUtc)).ToList(), page, size, total);
    }

    public async Task<PagedResult<SaleStockIssueResponse>> GetSaleIssuesAsync(int page, int size, CancellationToken ct)
    {
        (page, size) = Page(page, size);
        var query = store.StockMovements.AsNoTracking()
            .Where(movement => movement.Type == StockMovementType.SaleDispensed)
            .Join(store.InventoryItems, movement => movement.InventoryItemId, item => item.Id, (movement, item) => new { movement, item })
            .Join(store.Medicines, row => row.item.MedicineId, medicine => medicine.Id, (row, medicine) => new { row.movement, row.item, medicine })
            .Join(store.StockBatches, row => row.movement.StockBatchId, batch => batch.Id, (row, batch) => new { row.movement, row.item, row.medicine, batch });
        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(x => x.movement.OccurredAtUtc).Skip((page - 1) * size).Take(size).ToListAsync(ct);
        return new(rows.Select(x => new SaleStockIssueResponse(x.movement.Id, x.item.MedicineId, x.medicine.Name, x.batch.BatchNumber, x.batch.ExpiryDate, -x.movement.QuantityDelta, x.movement.QuantityBefore, x.movement.QuantityAfter, x.movement.SourceId, x.movement.OccurredAtUtc)).ToList(), page, size, total);
    }

    public Task<bool> ApplyPurchaseAsync(ExternalStockEvent message, CancellationToken ct) => Apply(message, "purchasing.purchase-recorded.v1", true, ct);
    public Task<bool> ApplySaleAsync(ExternalStockEvent message, CancellationToken ct) => Apply(message, "sales.sale-completed.v1", false, ct);

    private Task<bool> Apply(ExternalStockEvent message, string topic, bool increase, CancellationToken ct) => store.ExecuteTransactionAsync(async token =>
    {
        ValidateExternalEvent(message, increase);
        if (!await store.TryBeginEventAsync(message.EventId, topic, token)) return false;
        foreach (var line in message.Lines)
        {
            var item = await store.InventoryItems.Include(x => x.Batches).SingleOrDefaultAsync(x => x.MedicineId == line.MedicineId, token) ?? throw new NotFoundException($"Inventory for medicine {line.MedicineId} was not found.");
            var wasLow = item.IsLowStock;
            if (increase)
            {
                var batchNumber = line.BatchNumber!.Trim();
                var normalizedBatch = batchNumber.ToUpperInvariant();
                var batch = item.Batches.SingleOrDefault(x => x.NormalizedBatchNumber == normalizedBatch);
                if (batch is null)
                {
                    batch = new StockBatch(item.Id, batchNumber, line.Description ?? $"Purchase {message.SourceId}", line.ExpiryDate!.Value, line.Quantity);
                    await store.AddBatchAsync(batch, token);
                }
                else
                {
                    if (batch.ExpiryDate != line.ExpiryDate) throw new ConflictException($"Batch {batchNumber} has a different expiry date. The purchase was not applied.");
                    batch.Receive(line.Quantity);
                }
                item.ChangeStock(line.Quantity, StockMovementType.PurchaseReceived, "Purchase", message.SourceId, batch.Id);
            }
            else
            {
                var remaining = line.Quantity;
                foreach (var batch in item.Batches.Where(x => x.RemainingQuantity > 0 && x.ExpiryDate >= DateOnly.FromDateTime(DateTime.UtcNow)).OrderBy(x => x.ExpiryDate))
                { var take = Math.Min(remaining, batch.RemainingQuantity); batch.Consume(take); item.ChangeStock(-take, StockMovementType.SaleDispensed, "Sale", message.SourceId, batch.Id); remaining -= take; if (remaining == 0) break; }
                if (remaining > 0) throw new ConflictException("Insufficient sellable stock.");
            }
            QueueTransitions(item, wasLow, increase ? "Purchase" : "Sale", message.SourceId);
        }
        await store.SaveChangesAsync(token); return true;
    }, ct);

    private static void ValidateExternalEvent(ExternalStockEvent message, bool purchase)
    {
        var errors = new Dictionary<string, string[]>();
        if (message.EventId == Guid.Empty) errors["eventId"] = ["Event ID is required."];
        if (string.IsNullOrWhiteSpace(message.SourceId)) errors["sourceId"] = ["Source reference is required."];
        else if (message.SourceId.Trim().Length > StockMovement.MaxSourceIdLength) errors["sourceId"] = [$"Source reference cannot exceed {StockMovement.MaxSourceIdLength} characters."];
        if (message.Lines is null || message.Lines.Count == 0) errors["lines"] = ["At least one stock line is required."];
        else
        {
            for (var index = 0; index < message.Lines.Count; index++)
            {
                var line = message.Lines[index];
                var prefix = $"lines[{index}]";
                if (line.MedicineId == Guid.Empty) errors[$"{prefix}.medicineId"] = ["Medicine is required."];
                if (line.Quantity <= 0) errors[$"{prefix}.quantity"] = ["Quantity must be greater than zero."];
                if (!purchase) continue;
                if (string.IsNullOrWhiteSpace(line.BatchNumber)) errors[$"{prefix}.batchNumber"] = ["Batch number is required for a purchase."];
                else if (line.BatchNumber.Trim().Length > StockBatch.MaxBatchNumberLength) errors[$"{prefix}.batchNumber"] = [$"Batch number cannot exceed {StockBatch.MaxBatchNumberLength} characters."];
                if (line.ExpiryDate is null || line.ExpiryDate <= DateOnly.FromDateTime(DateTime.UtcNow)) errors[$"{prefix}.expiryDate"] = ["A future expiry date is required for a purchase."];
                if ((line.Description?.Trim().Length ?? 0) > StockBatch.MaxDescriptionLength) errors[$"{prefix}.description"] = [$"Description cannot exceed {StockBatch.MaxDescriptionLength} characters."];
            }
        }
        if (errors.Count > 0) throw new ValidationException(errors);
    }

    private static void ValidateBatch(RecordBatchRequest r)
    {
        var errors = new Dictionary<string, string[]>();
        if (r.MedicineId == Guid.Empty) errors["medicineId"] = ["Medicine is required."];
        if (string.IsNullOrWhiteSpace(r.BatchNumber)) errors["batchNumber"] = ["Batch number is required."];
        else if (r.BatchNumber.Trim().Length > StockBatch.MaxBatchNumberLength) errors["batchNumber"] = [$"Batch number cannot exceed {StockBatch.MaxBatchNumberLength} characters."];
        if ((r.Description?.Trim().Length ?? 0) > StockBatch.MaxDescriptionLength) errors["description"] = [$"Description cannot exceed {StockBatch.MaxDescriptionLength} characters."];
        if ((r.SourceReference?.Trim().Length ?? 0) > StockMovement.MaxSourceIdLength) errors["sourceReference"] = [$"Source reference cannot exceed {StockMovement.MaxSourceIdLength} characters."];
        if (r.Quantity <= 0) errors["quantity"] = ["Quantity must be greater than zero."];
        if (r.ExpiryDate <= DateOnly.FromDateTime(DateTime.UtcNow)) errors["expiryDate"] = ["Expiry date must be in the future."];
        if (errors.Count > 0) throw new ValidationException(errors);
    }

    private void QueueTransitions(InventoryItem i, bool wasLow, string reason, string source)
    {
        store.AddOutbox("inventory.stock-changed.v1", i.MedicineId.ToString(), "inventory.stock-changed.v1", new { i.MedicineId, i.QuantityOnHand, reason, source, occurredAtUtc = DateTime.UtcNow });
        if (!wasLow && i.IsLowStock) store.AddOutbox("inventory.low-stock-detected.v1", i.MedicineId.ToString(), "inventory.low-stock-detected.v1", new { i.MedicineId, i.QuantityOnHand, i.ReorderThreshold, detectedAtUtc = DateTime.UtcNow });
        if (wasLow && !i.IsLowStock) store.AddOutbox("inventory.stock-recovered.v1", i.MedicineId.ToString(), "inventory.stock-recovered.v1", new { i.MedicineId, i.QuantityOnHand, i.ReorderThreshold, recoveredAtUtc = DateTime.UtcNow });
    }

    private static BatchResponse MapBatch(StockBatch b, InventoryItem i, string name) => new(b.Id, i.MedicineId, name, b.BatchNumber, b.Description, b.ExpiryDate, b.InitialQuantity, b.RemainingQuantity, b.ReceivedAtUtc, i.QuantityOnHand, i.IsLowStock);
    private static InventoryResponse MapInventory(InventoryItem i, string n, string g) => new(i.MedicineId, n, g, i.QuantityOnHand, i.ReorderThreshold, i.IsLowStock, i.Batches.Where(b => b.RemainingQuantity > 0).Select(b => (DateOnly?)b.ExpiryDate).OrderBy(x => x).FirstOrDefault(), i.Version);
    private static (int, int) Page(int page, int size) => (Math.Max(page, 1), Math.Clamp(size, 1, 100));
    private static ConflictException DuplicateBatch() => new("This batch number already exists for the selected medicine. Stock was not changed.");
    private static ValidationException Invalid(string field, string message) => new(new Dictionary<string, string[]> { { field, [message] } });
}
