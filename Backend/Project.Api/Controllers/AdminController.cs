using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Application.Admin;
using Project.Application.HostApplications;

namespace Project.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly IHostApplicationService _applications;
    private readonly IAdminUserService _users;

    public AdminController(IHostApplicationService applications, IAdminUserService users)
    {
        _applications = applications;
        _users = users;
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
