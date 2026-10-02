using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Project.Application.Entities;
using Project.Application.Listings;

namespace Project.Api.Controllers;

[ApiController]
[Route("api/listings")]
public class ListingsController : ControllerBase
{
    private readonly IListingService _listings;

    public ListingsController(IListingService listings)
    {
        _listings = listings;
    }

    [HttpGet]
    public async Task<ActionResult<ListingSearchResultDto>> Search(
        [FromQuery] string? q,
        [FromQuery] string? city,
        [FromQuery] DateOnly? checkIn,
        [FromQuery] DateOnly? checkOut,
        [FromQuery] int? guests,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] ListingCategory[]? categories,
        [FromQuery] string? hostId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var result = await _listings.SearchAsync(
            new ListingSearchQuery(q, city, checkIn, checkOut, guests, minPrice, maxPrice, categories, hostId, page, pageSize),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ListingDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var listing = await _listings.GetByIdAsync(id, User.FindFirstValue(ClaimTypes.NameIdentifier), cancellationToken);
        return listing is null ? NotFound() : Ok(listing);
    }
}
