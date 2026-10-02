using Microsoft.AspNetCore.Mvc;
using Project.Application.Profiles;

namespace Project.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IProfileService _profiles;

    public UsersController(IProfileService profiles)
    {
        _profiles = profiles;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id, CancellationToken cancellationToken)
    {
        var profile = await _profiles.GetPublicAsync(id, cancellationToken);
        return profile is null ? NotFound() : Ok(profile);
    }
}
