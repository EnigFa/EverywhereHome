using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Project.Application.Auth;
using Project.Application.Entities;

namespace Project.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly IConfiguration _configuration;

    public AuthController(
        IAuthService authService,
        SignInManager<AppUser> signInManager,
        IConfiguration configuration)
    {
        _authService = authService;
        _signInManager = signInManager;
        _configuration = configuration;
    }

    [HttpGet("providers")]
    public ActionResult<AuthProvidersDto> Providers() => Ok(_authService.GetProviders());

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _authService.RegisterAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _authService.LoginAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpGet("external/{provider}")]
    public IActionResult External([FromRoute] string provider)
    {
        var providers = _authService.GetProviders();
        var scheme = provider.ToLowerInvariant() switch
        {
            "google" when providers.Google => GoogleDefaults.AuthenticationScheme,
            "facebook" when providers.Facebook => "Facebook",
            "apple" when providers.Apple => "Apple",
            _ => null
        };

        if (scheme is null)
        {
            return Redirect(FrontendCallback(error: ProviderNotReadyMessage(provider)));
        }

        var redirectUrl = Url.Action(nameof(ExternalCallback), "Auth", values: null, protocol: Request.Scheme);
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(scheme, redirectUrl);
        return Challenge(properties, scheme);
    }

    [AllowAnonymous]
    [HttpGet("external-callback")]
    public async Task<IActionResult> ExternalCallback()
    {
        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info is null)
        {
            return Redirect(FrontendCallback(error: "Не вдалося завершити вхід через зовнішній сервіс."));
        }

        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email))
        {
            return Redirect(FrontendCallback(error: "Провайдер не повернув email."));
        }

        var name = info.Principal.FindFirstValue(ClaimTypes.Name)
            ?? info.Principal.FindFirstValue(ClaimTypes.GivenName)
            ?? email;

        try
        {
            var result = await _authService.LoginOrRegisterExternalAsync(
                new ExternalLoginRequest(info.LoginProvider, info.ProviderKey, email, name));
            return Redirect(FrontendCallback(token: result.AccessToken));
        }
        catch (InvalidOperationException ex)
        {
            return Redirect(FrontendCallback(error: ex.Message));
        }
    }

    private string FrontendCallback(string? token = null, string? error = null)
    {
        var frontend = _configuration["Frontend:Url"]?.TrimEnd('/') ?? "http://localhost:5173";
        if (!string.IsNullOrWhiteSpace(error))
        {
            return $"{frontend}/auth/callback?error={Uri.EscapeDataString(error)}";
        }

        return $"{frontend}/auth/callback?token={Uri.EscapeDataString(token ?? string.Empty)}";
    }

    private static string ProviderNotReadyMessage(string provider) =>
        provider.ToLowerInvariant() switch
        {
            "google" => "Google вхід ще не налаштовано. Додайте ClientId і ClientSecret.",
            "apple" => "Вхід через Apple з’явиться після додавання ключів.",
            "facebook" => "Вхід через Facebook з’явиться після додавання ключів.",
            _ => "Цей спосіб входу не підтримується."
        };
}
