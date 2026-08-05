using Microsoft.AspNetCore.Mvc;
using TheHotelAPI.Application;

namespace TheHotelAPI.Api.Controllers;

[ApiController]
[Route("api/v1/hotel-searches")]
/// <summary>Exposes prompt-based hotel search as a resource-oriented POST operation.</summary>
public sealed class HotelSearchesController(HotelSearchService service) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<HotelSearchResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<HotelSearchResponse>> Search(SearchHotelsRequest request, CancellationToken ct) => Ok(await service.SearchAsync(request, ct));
}
