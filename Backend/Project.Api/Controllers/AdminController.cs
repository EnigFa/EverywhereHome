using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Application.Admin;
using Project.Application.HostApplications;
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

    public AdminController(
        IHostApplicationService applications,
        IAdminUserService users,
        IReportService reports,
        IConversationService conversations)
    {
        _applications = applications;
        _users = users;
        _reports = reports;
        _conversations = conversations;
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
    public Task<IActionResult> Users(CancellationToken cancellationToken) =>
        Run(id => _users.ListAsync(id, cancellationToken));

    [HttpPost("users/{id}/block")]
    public Task<IActionResult> Block(string id, CancellationToken cancellationToken) =>
        Run(adminId => _users.SetBlockedAsync(adminId, id, true, cancellationToken));

    [HttpPost("users/{id}/unblock")]
    public Task<IActionResult> Unblock(string id, CancellationToken cancellationToken) =>
        Run(adminId => _users.SetBlockedAsync(adminId, id, false, cancellationToken));

    [HttpGet("reports")]
    public Task<IActionResult> Reports(CancellationToken cancellationToken) =>
        Run(id => _reports.ListAsync(id, cancellationToken));

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
    public Task<IActionResult> Support(CancellationToken cancellationToken) =>
        Run(id => _conversations.ListSupportForAdminAsync(id, cancellationToken));

    [HttpGet("support/{userId}/messages")]
    public Task<IActionResult> SupportMessages(string userId, CancellationToken cancellationToken) =>
        Run(adminId => _conversations.ListSupportForAdminUserAsync(adminId, userId, cancellationToken));

    [HttpPost("support/{userId}/messages")]
    public Task<IActionResult> PostSupport(string userId, [FromBody] PostMessageRequest request, CancellationToken cancellationToken) =>
        Run(adminId => _conversations.PostSupportForAdminAsync(adminId, userId, request.Text, cancellationToken));

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
