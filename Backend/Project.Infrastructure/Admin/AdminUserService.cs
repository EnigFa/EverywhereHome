using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Project.Application.Admin;
using Project.Application.Entities;
using Project.Infrastructure.Data;

namespace Project.Infrastructure.Admin;

public class AdminUserService : IAdminUserService
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _users;

    public AdminUserService(AppDbContext db, UserManager<AppUser> users)
    {
        _db = db;
        _users = users;
    }

    public async Task<AdminSummaryDto> SummaryAsync(string adminId, CancellationToken cancellationToken = default)
    {
        await RequireAdminAsync(adminId);
        return new AdminSummaryDto(
            await _db.Reports.CountAsync(r => r.Status == ReportStatus.New, cancellationToken),
            await _db.Reports.CountAsync(r => r.Status == ReportStatus.InProgress, cancellationToken),
            await _db.Conversations.CountAsync(c => c.Kind == ConversationKind.Support && c.Status == CaseStatus.New, cancellationToken),
            await _db.Conversations.CountAsync(c => c.Kind == ConversationKind.Support && c.Status == CaseStatus.InProgress, cancellationToken),
            await _db.HostApplications.CountAsync(a => a.Status == HostApplicationStatus.Pending, cancellationToken),
            await _db.Users.CountAsync(u => u.IsBlocked, cancellationToken));
    }

    public async Task<AdminUserPageDto> ListAsync(string adminId, AdminUserQuery query, CancellationToken cancellationToken = default)
    {
        await RequireAdminAsync(adminId);
        var users = _db.Users.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            users = users.Where(u => u.Id.Contains(term) || (u.Email != null && u.Email.Contains(term)) || u.DisplayName.Contains(term));
        }

        if (query.Blocked is not null)
        {
            users = users.Where(u => u.IsBlocked == query.Blocked);
        }

        if (query.Trust is not null)
        {
            users = users.Where(u => u.TrustLevel == query.Trust);
        }

        users = query.Role switch
        {
            "host" => users.Where(u => u.IsHost),
            "guest" => users.Where(u => !u.IsHost),
            "admin" => users.Where(u => u.IsAdmin),
            _ => users
        };

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 50);
        var total = await users.CountAsync(cancellationToken);
        var items = await users
            .OrderBy(u => u.Email)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new AdminUserDto(u.Id, u.Email ?? string.Empty, u.DisplayName, u.IsHost, u.IsAdmin, u.IsBlocked, u.TrustLevel, u.IsChiefAdmin))
            .ToListAsync(cancellationToken);
        return new AdminUserPageDto(items, total, page, pageSize);
    }

    public async Task<AdminUserDto> SetBlockedAsync(string adminId, string userId, bool blocked, CancellationToken cancellationToken = default)
    {
        await RequireAdminAsync(adminId);
        if (adminId == userId)
        {
            throw new InvalidOperationException("Не можна заблокувати власний акаунт.");
        }

        var user = await _users.FindByIdAsync(userId) ?? throw new InvalidOperationException("Користувача не знайдено.");
        if (user.IsAdmin || user.IsChiefAdmin)
        {
            throw new InvalidOperationException("Адміністратора заблокувати не можна.");
        }

        user.IsBlocked = blocked;
        if (blocked)
        {
            var listings = await _db.Listings
                .Where(l => l.HostId == user.Id && l.IsPublished)
                .ToListAsync(cancellationToken);
            foreach (var listing in listings)
            {
                listing.IsPublished = false;
            }
        }

        var update = await _users.UpdateAsync(user);
        if (!update.Succeeded)
        {
            throw new InvalidOperationException(string.Join(" ", update.Errors.Select(e => e.Description)));
        }

        return Map(user);
    }

    public async Task<AdminUserDto> SetTrustAsync(string adminId, string userId, int trustLevel, CancellationToken cancellationToken = default)
    {
        await RequireAdminAsync(adminId);
        if (trustLevel is < 0 or > 2)
        {
            throw new InvalidOperationException("Невідомий статус надійності.");
        }

        var user = await _users.FindByIdAsync(userId) ?? throw new InvalidOperationException("Користувача не знайдено.");
        user.TrustLevel = trustLevel;
        var update = await _users.UpdateAsync(user);
        if (!update.Succeeded)
        {
            throw new InvalidOperationException(string.Join(" ", update.Errors.Select(e => e.Description)));
        }

        return Map(user);
    }

    public async Task<AdminUserDto> SetRoleAsync(string adminId, string userId, string role, CancellationToken cancellationToken = default)
    {
        var actor = await _users.FindByIdAsync(adminId) ?? throw new UnauthorizedAccessException("Ця дія доступна лише адміністратору.");
        if (actor.IsBlocked || (!actor.IsAdmin && !actor.IsChiefAdmin))
        {
            throw new UnauthorizedAccessException("Ця дія доступна лише адміністратору.");
        }

        var user = await _users.FindByIdAsync(userId) ?? throw new InvalidOperationException("Користувача не знайдено.");
        if (user.IsChiefAdmin || user.Id == actor.Id)
        {
            throw new InvalidOperationException("Цю роль змінити не можна.");
        }

        var next = role.Trim().ToLowerInvariant();
        if (next is not ("guest" or "host" or "admin"))
        {
            throw new InvalidOperationException("Невідома роль.");
        }

        if (next == "admin" && !actor.IsChiefAdmin)
        {
            throw new UnauthorizedAccessException("Статус адміністратора призначає лише головний адміністратор.");
        }

        if ((user.IsAdmin || user.IsChiefAdmin) && !actor.IsChiefAdmin)
        {
            throw new UnauthorizedAccessException("Роль адміністратора може змінити лише головний адміністратор.");
        }

        user.IsAdmin = next == "admin";
        user.IsHost = next is "host" or "admin" ? user.IsHost || next == "host" : false;
        if (next == "guest")
        {
            user.IsHost = false;
        }

        if (next == "host")
        {
            user.IsHost = true;
            user.IsAdmin = false;
        }

        var update = await _users.UpdateAsync(user);
        if (!update.Succeeded)
        {
            throw new InvalidOperationException(string.Join(" ", update.Errors.Select(e => e.Description)));
        }

        return Map(user);
    }

    private static AdminUserDto Map(AppUser user) =>
        new(user.Id, user.Email ?? string.Empty, user.DisplayName, user.IsHost, user.IsAdmin, user.IsBlocked, user.TrustLevel, user.IsChiefAdmin);

    private async Task RequireAdminAsync(string adminId)
    {
        var admin = await _users.FindByIdAsync(adminId);
        if (admin is null || (!admin.IsAdmin && !admin.IsChiefAdmin) || admin.IsBlocked)
        {
            throw new UnauthorizedAccessException("Ця дія доступна лише адміністратору.");
        }
    }
}
