using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Application.Admin;
using Project.Application.HostApplications;
using Project.Application.HostListings;
using Project.Application.Messages;
using Project.Application.Reports;

namespace Project.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly IHostApplicationService _applications;
    private readonly IAdminUserService _users;
    private readonly IReportService _reports;
    private readonly IConversationService _conversations;
    private readonly IHostListingService _listings;

    public AdminController(
        IHostApplicationService applications,
        IAdminUserService users,
        IReportService reports,
        IConversationService conversations,
        IHostListingService listings)
    {
        _applications = applications;
        _users = users;
        _reports = reports;
        _conversations = conversations;
        _listings = listings;
    }

    [HttpGet("host-applications")]
    public Task<IActionResult> Applications(CancellationToken cancellationToken) =>
        Run(id => _applications.ListAsync(id, cancellationToken));

    [HttpPost("host-applications/{id:guid}/approve")]
    public Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken) =>
        Run(adminId => _applications.ApproveAsync(adminId, id, cancellationToken));

    [HttpPost("host-applications/{id:guid}/reject")]
    public Task<IActionResult> Reject(Guid id, [FromBody] RejectHostApplicationRequest? request, CancellationToken cancellationToken) =>
        Run(adminId => _applications.RejectAsync(adminId, id, request?.Note, cancellationToken));

    [HttpGet("users")]
    public Task<IActionResult> Users([FromQuery] string? q, [FromQuery] bool? blocked, [FromQuery] int? trust, [FromQuery] string? role, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken cancellationToken = default) =>
        Run(id => _users.ListAsync(id, new AdminUserQuery(q, blocked, trust, role, page, pageSize), cancellationToken));

    [HttpPost("users/{id}/trust")]
    public Task<IActionResult> Trust(string id, [FromBody] SetTrustRequest request, CancellationToken cancellationToken) =>
        Run(adminId => _users.SetTrustAsync(adminId, id, request.TrustLevel, cancellationToken));

    [HttpPost("users/{id}/block")]
    public Task<IActionResult> Block(string id, CancellationToken cancellationToken) =>
        Run(adminId => _users.SetBlockedAsync(adminId, id, true, cancellationToken));

    [HttpPost("users/{id}/unblock")]
    public Task<IActionResult> Unblock(string id, CancellationToken cancellationToken) =>
        Run(adminId => _users.SetBlockedAsync(adminId, id, false, cancellationToken));

    [HttpGet("summary")]
    public Task<IActionResult> Summary(CancellationToken cancellationToken) =>
        Run(id => _users.SummaryAsync(id, cancellationToken));

    [HttpGet("reports")]
    public Task<IActionResult> Reports([FromQuery] int? status, CancellationToken cancellationToken) =>
        Run(id => _reports.ListAsync(id, status, cancellationToken));

    [HttpGet("reports/{id:guid}")]
    public Task<IActionResult> Report(Guid id, CancellationToken cancellationToken) =>
        Run(adminId => _reports.GetAsync(adminId, id, cancellationToken));

    [HttpPost("reports/{id:guid}/take")]
    public Task<IActionResult> TakeReport(Guid id, CancellationToken cancellationToken) =>
        Run(adminId => _reports.TakeAsync(adminId, id, cancellationToken));

    [HttpPost("reports/{id:guid}/release")]
    public Task<IActionResult> ReleaseReport(Guid id, CancellationToken cancellationToken) =>
        Run(adminId => _reports.ReleaseAsync(adminId, id, cancellationToken));

    [HttpPost("reports/{id:guid}/resolve")]
    public Task<IActionResult> ResolveReport(Guid id, [FromBody] Project.Application.Reports.ResolveReportRequest request, CancellationToken cancellationToken) =>
        Run(adminId => _reports.ResolveAsync(adminId, id, request, cancellationToken));

    [HttpPost("reports/{id:guid}/review")]
    public Task<IActionResult> ReviewReport(Guid id, CancellationToken cancellationToken) =>
        Run(adminId => _reports.MarkReviewedAsync(adminId, id, cancellationToken));

    [HttpPost("reports/{id:guid}/unpublish")]
    public Task<IActionResult> UnpublishFromReport(Guid id, CancellationToken cancellationToken) =>
        Run(adminId => _reports.UnpublishFromReportAsync(adminId, id, cancellationToken));

    [HttpPost("reports/{id:guid}/block")]
    public Task<IActionResult> BlockFromReport(Guid id, CancellationToken cancellationToken) =>
        Run(adminId => _reports.BlockFromReportAsync(adminId, id, cancellationToken));

    [HttpPost("listings/{id:guid}/unpublish")]
    public async Task<IActionResult> Unpublish(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        try
        {
            await _reports.UnpublishListingAsync(userId, id, cancellationToken);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("support")]
    public Task<IActionResult> Support([FromQuery] int? status, CancellationToken cancellationToken) =>
        Run(id => _conversations.ListSupportTicketsAsync(id, status, cancellationToken));

    [HttpPost("support/{id:guid}/take")]
    public Task<IActionResult> TakeSupport(Guid id, CancellationToken cancellationToken) =>
        Run(adminId => _conversations.TakeSupportAsync(adminId, id, cancellationToken));

    [HttpPost("support/{id:guid}/release")]
    public Task<IActionResult> ReleaseSupport(Guid id, CancellationToken cancellationToken) =>
        Run(adminId => _conversations.ReleaseSupportAsync(adminId, id, cancellationToken));

    [HttpPost("support/{id:guid}/resolve")]
    public Task<IActionResult> ResolveSupport(Guid id, [FromBody] ResolveSupportRequest request, CancellationToken cancellationToken) =>
        Run(adminId => _conversations.ResolveSupportAsync(adminId, id, request.Decision, cancellationToken));

    [HttpGet("support/{userId}/messages")]
    public Task<IActionResult> SupportMessages(string userId, CancellationToken cancellationToken) =>
        Run(adminId => _conversations.ListSupportForAdminUserAsync(adminId, userId, cancellationToken));

    [HttpPost("support/{userId}/messages")]
    public Task<IActionResult> PostSupport(string userId, [FromBody] PostMessageRequest request, CancellationToken cancellationToken) =>
        Run(adminId => _conversations.PostSupportForAdminAsync(adminId, userId, request.Text, cancellationToken));

    [HttpGet("listings/{id:guid}")]
    public Task<IActionResult> Listing(Guid id, CancellationToken cancellationToken) =>
        Run(adminId => _listings.GetAnyAsync(adminId, id, cancellationToken));

    [HttpPut("listings/{id:guid}")]
    public Task<IActionResult> UpdateListing(Guid id, [FromBody] HostListingInput input, CancellationToken cancellationToken) =>
        Run(adminId => _listings.UpdateAnyAsync(adminId, id, input, cancellationToken));

    [HttpPost("listings/{id:guid}/photos")]
    [RequestSizeLimit(5_000_000)]
    public async Task<IActionResult> AddListingPhoto(Guid id, [FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
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
            return Ok(await _listings.AddPhotoAnyAsync(userId, id, stream, file.FileName, cancellationToken));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("listings/{id:guid}/photos/{photoId:guid}")]
    public Task<IActionResult> DeleteListingPhoto(Guid id, Guid photoId, CancellationToken cancellationToken) =>
        Run(async adminId =>
        {
            await _listings.DeletePhotoAnyAsync(adminId, id, photoId, cancellationToken);
            return new { ok = true };
        });

    [HttpPost("users/{id}/role")]
    public Task<IActionResult> SetRole(string id, [FromBody] SetRoleRequest request, CancellationToken cancellationToken) =>
        Run(adminId => _users.SetRoleAsync(adminId, id, request.Role, cancellationToken));

    private async Task<IActionResult> Run<T>(Func<string, Task<T>> action)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await action(userId));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

public record RejectHostApplicationRequest(string? Note);
public record SetTrustRequest(int TrustLevel);
public record SetRoleRequest(string Role);
public record ResolveSupportRequest(string Decision);
