using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Application.Favorites;
using Project.Application.Listings;

namespace Project.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/favorites")]
public class FavoritesController : ControllerBase
{
    private readonly IFavoriteService _favorites;

    public FavoritesController(IFavoriteService favorites)
    {
        _favorites = favorites;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ListingCardDto>>> Mine(CancellationToken cancellationToken)
    {
        var userId = UserId();
        return userId is null ? Unauthorized() : Ok(await _favorites.ListMineAsync(userId, cancellationToken));
    }

    [HttpPost("{listingId:guid}")]
    public async Task<IActionResult> Add(Guid listingId, CancellationToken cancellationToken)
    {
        var userId = UserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        try
        {
            await _favorites.AddAsync(userId, listingId, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{listingId:guid}")]
    public async Task<IActionResult> Remove(Guid listingId, CancellationToken cancellationToken)
    {
        var userId = UserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        await _favorites.RemoveAsync(userId, listingId, cancellationToken);
        return NoContent();
    }

    private string? UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);
}
