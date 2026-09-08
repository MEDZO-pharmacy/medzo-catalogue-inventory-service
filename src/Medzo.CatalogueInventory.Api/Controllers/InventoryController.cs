using Medzo.CatalogueInventory.Application.Common;using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;
namespace Medzo.CatalogueInventory.Api.Controllers;
[ApiController,Route("api/inventory"),Authorize(Policy="InventoryRead")]
public sealed class InventoryController(IInventoryService service):ControllerBase{
 [HttpGet("items")]public Task<PagedResult<InventoryResponse>> Items([FromQuery]string? search,[FromQuery]bool lowStock=false,[FromQuery]int page=1,[FromQuery]int pageSize=20,CancellationToken ct=default)=>service.GetAsync(search,lowStock,page,pageSize,ct);
 [HttpGet("items/low-stock"),Authorize(Policy="InventoryManage")]public Task<PagedResult<InventoryResponse>> Low([FromQuery]int page=1,[FromQuery]int pageSize=20,CancellationToken ct=default)=>service.GetAsync(null,true,page,pageSize,ct);
 [HttpGet("items/{medicineId:guid}/movements")]public Task<PagedResult<MovementResponse>> Movements(Guid medicineId,[FromQuery]int page=1,[FromQuery]int pageSize=20,CancellationToken ct=default)=>service.GetMovementsAsync(medicineId,page,pageSize,ct);
 [HttpGet("purchase-receipts"),Authorize(Policy="InventoryManage")]public Task<PagedResult<PurchaseStockReceiptResponse>> PurchaseReceipts([FromQuery]int page=1,[FromQuery]int pageSize=20,CancellationToken ct=default)=>service.GetPurchaseReceiptsAsync(page,pageSize,ct);
 [HttpGet("batches"),Authorize(Policy="InventoryManage")]public Task<PagedResult<BatchResponse>> Batches([FromQuery]Guid? medicineId,[FromQuery]int page=1,[FromQuery]int pageSize=20,CancellationToken ct=default)=>service.GetBatchesAsync(medicineId,page,pageSize,ct);
 [HttpGet("batches/{id:guid}"),Authorize(Policy="InventoryManage")]public Task<BatchResponse> Batch(Guid id,CancellationToken ct)=>service.GetBatchAsync(id,ct);
 [HttpPost("batches"),Authorize(Policy="InventoryManage")]public async Task<ActionResult<BatchResponse>> RecordBatch(RecordBatchRequest r,CancellationToken ct){var result=await service.RecordBatchAsync(r,ct);return CreatedAtAction(nameof(Batch),new{id=result.Id},result);}}
