using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Application.HostApplications;

namespace Project.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/host/application")]
public class HostApplicationsController : ControllerBase
{
    private readonly IHostApplicationService _applications;

    public HostApplicationsController(IHostApplicationService applications)
    {
        _applications = applications;
    }

    [HttpGet]
    public async Task<IActionResult> Mine(CancellationToken cancellationToken)
    {
        var userId = UserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var application = await _applications.GetMineAsync(userId, cancellationToken);
        return application is null ? NoContent() : Ok(application);
    }

    [HttpPost]
    [RequestSizeLimit(5_000_000)]
    public async Task<IActionResult> Submit([FromForm] string fullName, [FromForm] IFormFile document, CancellationToken cancellationToken)
    {
        var userId = UserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        if (document is null || document.Length == 0)
        {
            return BadRequest(new { message = "Додайте фото або PDF документа." });
        }

        try
        {
            await using var stream = document.OpenReadStream();
            return Ok(await _applications.SubmitAsync(userId, fullName, stream, document.FileName, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private string? UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);
}
