using Medzo.CatalogueInventory.Application.Common;using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;
namespace Medzo.CatalogueInventory.Api.Controllers;
[ApiController,Route("api/catalogue/medicines"),Authorize(Policy="CatalogueRead")]
public sealed class CatalogueController(ICatalogueService service):ControllerBase{
 [HttpGet]public Task<PagedResult<MedicineResponse>> Search([FromQuery]string? search,[FromQuery]int page=1,[FromQuery]int pageSize=20,CancellationToken ct=default)=>service.SearchAsync(search,page,pageSize,ct);
 [HttpGet("{id:guid}")]public Task<MedicineResponse> Get(Guid id,CancellationToken ct)=>service.GetAsync(id,ct);
 [HttpPost,Authorize(Policy="InventoryManage")]public async Task<ActionResult<MedicineResponse>> Create(MedicineRequest r,CancellationToken ct){var result=await service.CreateAsync(r,ct);return CreatedAtAction(nameof(Get),new{id=result.Id},result);}
 [HttpPut("{id:guid}"),Authorize(Policy="InventoryManage")]public Task<MedicineResponse> Update(Guid id,MedicineRequest r,[FromHeader(Name="If-Match")]long version,CancellationToken ct)=>service.UpdateAsync(id,r,version,ct);}

