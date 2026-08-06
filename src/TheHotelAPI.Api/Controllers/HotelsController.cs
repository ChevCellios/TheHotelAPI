using Microsoft.AspNetCore.Mvc;
using TheHotelAPI.Application;

namespace TheHotelAPI.Api.Controllers;

[ApiController]
[Route("api/v1/hotels")]
/// <summary>Exposes hotel management and paginated listing operations.</summary>
public sealed class HotelsController(HotelService service) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<HotelResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<HotelResponse>> Create(UpsertHotelRequest request, CancellationToken ct)
    { var hotel = await service.CreateAsync(request, ct); return CreatedAtAction(nameof(Get), new { id = hotel.Id }, hotel); }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<HotelResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HotelResponse>> Get(Guid id, CancellationToken ct)
    { var hotel = await service.GetAsync(id, ct); return hotel is null ? NotFound() : Ok(hotel); }

    [HttpGet]
    [ProducesResponseType<PagedResult<HotelResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<HotelResponse>>> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await service.ListAsync(page, pageSize, ct));

    [HttpPut("{id:guid}")]
    [ProducesResponseType<HotelResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HotelResponse>> Update(Guid id, UpsertHotelRequest request, CancellationToken ct)
    { var hotel = await service.UpdateAsync(id, request, ct); return hotel is null ? NotFound() : Ok(hotel); }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) => await service.DeleteAsync(id, ct) ? NoContent() : NotFound();
}
