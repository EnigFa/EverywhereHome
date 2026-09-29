using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Application.Listings;
using Project.Application.Reviews;

namespace Project.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/listings/{listingId:guid}/reviews")]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviews;

    public ReviewsController(IReviewService reviews)
    {
        _reviews = reviews;
    }

    [HttpPost]
    public async Task<ActionResult<ReviewItemDto>> Create(Guid listingId, [FromBody] CreateReviewRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await _reviews.CreateAsync(userId, listingId, request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
