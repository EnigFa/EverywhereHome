using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Project.Application.Auth;
using Project.Application.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Project.Infrastructure.Auth;

public class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IConfiguration _configuration;

    public AuthService(UserManager<AppUser> userManager, IConfiguration configuration)
    {
        _userManager = userManager;
        _configuration = configuration;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password != request.ConfirmPassword)
        {
            throw new InvalidOperationException("Паролі не збігаються.");
        }

        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
        {
            throw new InvalidOperationException("Користувач з такою поштою вже існує.");
        }

        var user = new AppUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(" ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException(errors);
        }

        return CreateResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            throw new UnauthorizedAccessException("Невірна пошта або пароль.");
        }

        return CreateResponse(user);
    }

    public async Task<AuthResponse> LoginOrRegisterExternalAsync(
        ExternalLoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var login = new UserLoginInfo(request.Provider, request.ProviderKey, request.Provider);
        var existingByLogin = await _userManager.FindByLoginAsync(request.Provider, request.ProviderKey);
        if (existingByLogin is not null)
        {
            return CreateResponse(existingByLogin);
        }

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            user = new AppUser
            {
                UserName = request.Email,
                Email = request.Email,
                DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? request.Email : request.DisplayName,
                EmailConfirmed = true
            };

            var create = await _userManager.CreateAsync(user);
            if (!create.Succeeded)
            {
                throw new InvalidOperationException(string.Join(" ", create.Errors.Select(e => e.Description)));
            }
        }

        var addLogin = await _userManager.AddLoginAsync(user, login);
        if (!addLogin.Succeeded)
        {
            throw new InvalidOperationException(string.Join(" ", addLogin.Errors.Select(e => e.Description)));
        }

        return CreateResponse(user);
    }

    public AuthProvidersDto GetProviders()
    {
        var google = !string.IsNullOrWhiteSpace(_configuration["Authentication:Google:ClientId"]);
        var facebook = !string.IsNullOrWhiteSpace(_configuration["Authentication:Facebook:AppId"]);
        var apple = !string.IsNullOrWhiteSpace(_configuration["Authentication:Apple:ClientId"]);
        return new AuthProvidersDto(google, facebook, apple);
    }

    private AuthResponse CreateResponse(AppUser user)
    {
        var jwt = _configuration.GetSection("Jwt");
        var key = jwt["Key"] ?? throw new InvalidOperationException("Jwt:Key is missing.");
        var issuer = jwt["Issuer"] ?? "EverywhereHome";
        var audience = jwt["Audience"] ?? "EverywhereHome";
        var expiresMinutes = int.TryParse(jwt["ExpiresMinutes"], out var minutes) ? minutes : 120;

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim("displayName", user.DisplayName),
            new Claim(ClaimTypes.NameIdentifier, user.Id)
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        return new AuthResponse(accessToken, user.DisplayName, user.Email ?? string.Empty, user.IsHost);
    }
}
