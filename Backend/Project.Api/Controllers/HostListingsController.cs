using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Application.HostListings;

namespace Project.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/host/listings")]
public class HostListingsController : ControllerBase
{
    private readonly IHostListingService _listings;

    public HostListingsController(IHostListingService listings)
    {
        _listings = listings;
    }

    [HttpGet]
    public async Task<IActionResult> Mine(CancellationToken cancellationToken)
    {
        var userId = UserId();
        return userId is null ? Unauthorized() : Ok(await _listings.ListMineAsync(userId, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var userId = UserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await _listings.GetMineAsync(userId, id, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] HostListingInput input, CancellationToken cancellationToken)
    {
        var userId = UserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        try
        {
            var created = await _listings.CreateAsync(userId, input, cancellationToken);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] HostListingInput input, CancellationToken cancellationToken)
    {
        var userId = UserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await _listings.UpdateAsync(userId, id, input, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = UserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        try
        {
            await _listings.DeleteAsync(userId, id, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/photos")]
    [RequestSizeLimit(5_000_000)]
    public async Task<IActionResult> AddPhoto(Guid id, [FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        var userId = UserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "Оберіть файл зображення." });
        }

        try
        {
            await using var stream = file.OpenReadStream();
            return Ok(await _listings.AddPhotoAsync(userId, id, stream, file.FileName, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}/photos/{photoId:guid}")]
    public async Task<IActionResult> DeletePhoto(Guid id, Guid photoId, CancellationToken cancellationToken)
    {
        var userId = UserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        try
        {
            await _listings.DeletePhotoAsync(userId, id, photoId, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private string? UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);
}
