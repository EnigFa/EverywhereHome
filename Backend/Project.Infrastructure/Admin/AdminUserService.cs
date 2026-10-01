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

    public async Task<IReadOnlyList<AdminUserDto>> ListAsync(string adminId, CancellationToken cancellationToken = default)
    {
        await RequireAdminAsync(adminId);
        return await _db.Users
            .AsNoTracking()
            .OrderBy(u => u.Email)
            .Select(u => new AdminUserDto(u.Id, u.Email ?? string.Empty, u.DisplayName, u.IsHost, u.IsAdmin, u.IsBlocked))
            .ToListAsync(cancellationToken);
    }

    public async Task<AdminUserDto> SetBlockedAsync(string adminId, string userId, bool blocked, CancellationToken cancellationToken = default)
    {
        await RequireAdminAsync(adminId);
        if (adminId == userId)
        {
            throw new InvalidOperationException("Не можна заблокувати власний акаунт.");
        }

        var user = await _users.FindByIdAsync(userId) ?? throw new InvalidOperationException("Користувача не знайдено.");
        if (user.IsAdmin)
        {
            throw new InvalidOperationException("Адміністратора заблокувати не можна.");
        }

        user.IsBlocked = blocked;
        var update = await _users.UpdateAsync(user);
        if (!update.Succeeded)
        {
            throw new InvalidOperationException(string.Join(" ", update.Errors.Select(e => e.Description)));
        }

        return new AdminUserDto(user.Id, user.Email ?? string.Empty, user.DisplayName, user.IsHost, user.IsAdmin, user.IsBlocked);
    }

    private async Task RequireAdminAsync(string adminId)
    {
        var admin = await _users.FindByIdAsync(adminId);
        if (admin is null || !admin.IsAdmin || admin.IsBlocked)
        {
            throw new UnauthorizedAccessException("Ця дія доступна лише адміністратору.");
        }
    }
}
