namespace Project.Application.Admin;

public record AdminUserDto(string Id, string Email, string DisplayName, bool IsHost, bool IsAdmin, bool IsBlocked);

public interface IAdminUserService
{
    Task<IReadOnlyList<AdminUserDto>> ListAsync(string adminId, CancellationToken cancellationToken = default);
    Task<AdminUserDto> SetBlockedAsync(string adminId, string userId, bool blocked, CancellationToken cancellationToken = default);
}
