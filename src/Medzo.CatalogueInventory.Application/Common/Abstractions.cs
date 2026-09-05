using Medzo.CatalogueInventory.Domain.Catalogue;using Medzo.CatalogueInventory.Domain.Inventory;
namespace Medzo.CatalogueInventory.Application.Common;
public interface ICatalogueInventoryStore{
 IQueryable<Medicine> Medicines{get;} IQueryable<InventoryItem> InventoryItems{get;} IQueryable<StockBatch> StockBatches{get;} IQueryable<StockMovement> StockMovements{get;}
 Task AddMedicineAsync(Medicine medicine,CancellationToken ct);Task AddInventoryItemAsync(InventoryItem item,CancellationToken ct);Task AddBatchAsync(StockBatch batch,CancellationToken ct);Task<int> SaveChangesAsync(CancellationToken ct);
 Task<T> ExecuteTransactionAsync<T>(Func<CancellationToken,Task<T>> action,CancellationToken ct);
 Task<bool> TryBeginEventAsync(Guid eventId,string topic,CancellationToken ct);void AddOutbox(string topic,string key,string type,object data);
}
public interface ICatalogueService{Task<PagedResult<MedicineResponse>> SearchAsync(string? search,int page,int pageSize,CancellationToken ct);Task<MedicineResponse> GetAsync(Guid id,CancellationToken ct);Task<MedicineResponse> CreateAsync(MedicineRequest request,CancellationToken ct);Task<MedicineResponse> UpdateAsync(Guid id,MedicineRequest request,long version,CancellationToken ct);}
public interface IInventoryService{Task<PagedResult<InventoryResponse>> GetAsync(string? search,bool lowStock,int page,int pageSize,CancellationToken ct);Task<PagedResult<BatchResponse>> GetBatchesAsync(Guid? medicineId,int page,int pageSize,CancellationToken ct);Task<BatchResponse> GetBatchAsync(Guid id,CancellationToken ct);Task<BatchResponse> RecordBatchAsync(RecordBatchRequest request,CancellationToken ct);Task<PagedResult<MovementResponse>> GetMovementsAsync(Guid medicineId,int page,int pageSize,CancellationToken ct);Task<bool> ApplyPurchaseAsync(ExternalStockEvent message,CancellationToken ct);Task<bool> ApplySaleAsync(ExternalStockEvent message,CancellationToken ct);}

