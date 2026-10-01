namespace Project.Application.Auth;

public record RegisterRequest(string Email, string Password, string ConfirmPassword, string DisplayName);

public record LoginRequest(string Email, string Password);

public record AuthResponse(string AccessToken, string DisplayName, string Email, bool IsHost);

public record ExternalLoginRequest(string Provider, string ProviderKey, string Email, string DisplayName);

public record AuthProvidersDto(bool Google, bool Facebook, bool Apple);

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> LoginOrRegisterExternalAsync(ExternalLoginRequest request, CancellationToken cancellationToken = default);
    AuthProvidersDto GetProviders();
}
