using Medzo.CatalogueInventory.Domain.Catalogue;
namespace Medzo.CatalogueInventory.Application.Common;
public sealed record PagedResult<T>(IReadOnlyList<T> Items,int Page,int PageSize,int TotalCount);
public sealed record MedicineRequest(string Name,string GenericName,string Manufacturer,decimal UnitPrice,DosageForm DosageForm,Guid? CategoryId,int ReorderThreshold);
public sealed record MedicineResponse(Guid Id,string Name,string GenericName,string Manufacturer,decimal UnitPrice,DosageForm DosageForm,Guid? CategoryId,string? CategoryName,bool IsActive,int QuantityOnHand,int ReorderThreshold,bool IsLowStock,long Version);
public sealed record RecordBatchRequest(Guid MedicineId,string BatchNumber,string? Description,DateOnly ExpiryDate,int Quantity,string? SourceReference);
public sealed record BatchResponse(Guid Id,Guid MedicineId,string MedicineName,string BatchNumber,string? Description,DateOnly ExpiryDate,int InitialQuantity,int RemainingQuantity,DateTime ReceivedAtUtc,int QuantityOnHand,bool IsLowStock);
public sealed record InventoryResponse(Guid MedicineId,string Name,string GenericName,int QuantityOnHand,int ReorderThreshold,bool IsLowStock,DateOnly? NextExpiry,long Version);
public sealed record MovementResponse(Guid Id,string Type,int QuantityDelta,int QuantityBefore,int QuantityAfter,string SourceType,string SourceId,string? BatchNumber,DateTime OccurredAtUtc);
public sealed record PurchaseStockReceiptResponse(Guid MovementId,Guid MedicineId,string MedicineName,string BatchNumber,DateOnly ExpiryDate,int QuantityReceived,int QuantityBefore,int QuantityAfter,string PurchaseReference,DateTime ProcessedAtUtc);
public sealed record SaleStockIssueResponse(Guid MovementId,Guid MedicineId,string MedicineName,string BatchNumber,DateOnly ExpiryDate,int QuantitySold,int QuantityBefore,int QuantityAfter,string SaleReference,DateTime ProcessedAtUtc);
public sealed record ExternalStockLine(Guid MedicineId,int Quantity,string? BatchNumber,string? Description,DateOnly? ExpiryDate);
public sealed record ExternalStockEvent(Guid EventId,string SourceId,DateTime OccurredAtUtc,IReadOnlyList<ExternalStockLine> Lines);

