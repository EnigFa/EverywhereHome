namespace Project.Application.Admin;

public record AdminUserDto(string Id, string Email, string DisplayName, bool IsHost, bool IsAdmin, bool IsBlocked, int TrustLevel, bool IsChiefAdmin);

public record AdminUserQuery(string? Search, bool? Blocked, int? Trust, string? Role, int Page, int PageSize);

public record AdminUserPageDto(IReadOnlyList<AdminUserDto> Items, int Total, int Page, int PageSize);

public record AdminSummaryDto(int ReportQueue, int ReportInProgress, int SupportQueue, int SupportInProgress, int PendingApplications, int BlockedUsers);

public interface IAdminUserService
{
    Task<AdminSummaryDto> SummaryAsync(string adminId, CancellationToken cancellationToken = default);
    Task<AdminUserPageDto> ListAsync(string adminId, AdminUserQuery query, CancellationToken cancellationToken = default);
    Task<AdminUserDto> SetBlockedAsync(string adminId, string userId, bool blocked, CancellationToken cancellationToken = default);
    Task<AdminUserDto> SetTrustAsync(string adminId, string userId, int trustLevel, CancellationToken cancellationToken = default);
    Task<AdminUserDto> SetRoleAsync(string adminId, string userId, string role, CancellationToken cancellationToken = default);
}
