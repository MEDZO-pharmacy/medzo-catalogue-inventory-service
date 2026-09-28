using Medzo.CatalogueInventory.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Medzo.CatalogueInventory.Api.Controllers;

[ApiController,Route("api/batch-removals"),Authorize(Policy="InventoryManage")]
public sealed class BatchRemovalsController(IInventoryService service):ControllerBase
{
    [HttpGet("candidates")]
    public Task<PagedResult<BatchRemovalCandidateResponse>> Candidates([FromQuery]int withinDays=30,[FromQuery]int page=1,[FromQuery]int pageSize=50,CancellationToken ct=default)=>
        service.GetBatchRemovalCandidatesAsync(withinDays,page,pageSize,ct);

    [HttpPost("{batchId:guid}")]
    public async Task<ActionResult<BatchRemovalResponse>> Remove(Guid batchId,RemoveBatchRequest request,CancellationToken ct)
    {
        var result=await service.RemoveBatchAsync(batchId,request,ct);
        return result.AlreadyRemoved?Ok(result):Created($"/api/batch-removals/{batchId}",result);
    }
}
