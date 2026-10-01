using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Project.Application.Entities;
using Project.Application.Files;
using Project.Application.HostApplications;
using Project.Infrastructure.Data;

namespace Project.Infrastructure.HostApplications;

public class HostApplicationService : IHostApplicationService
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _users;
    private readonly IFileStorage _files;

    public HostApplicationService(AppDbContext db, UserManager<AppUser> users, IFileStorage files)
    {
        _db = db;
        _users = users;
        _files = files;
    }

    public async Task<HostApplicationDto?> GetMineAsync(string userId, CancellationToken cancellationToken = default)
    {
        var application = await _db.HostApplications
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        return application is null ? null : Map(application, null, null);
    }

    public async Task<HostApplicationDto> SubmitAsync(string userId, string fullName, Stream document, string fileName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new InvalidOperationException("Вкажіть імʼя, як у документі.");
        }

        var user = await _users.FindByIdAsync(userId) ?? throw new InvalidOperationException("Користувача не знайдено.");
        if (user.IsBlocked)
        {
            throw new InvalidOperationException("Акаунт заблоковано.");
        }

        if (user.IsHost)
        {
            throw new InvalidOperationException("Ви вже маєте статус господаря.");
        }

        var pending = await _db.HostApplications.AnyAsync(
            x => x.UserId == userId && x.Status == HostApplicationStatus.Pending,
            cancellationToken);
        if (pending)
        {
            throw new InvalidOperationException("Заявка вже на розгляді.");
        }

        var url = await _files.SaveHostDocumentAsync(document, fileName, cancellationToken);
        var application = new HostApplication
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FullName = fullName.Trim(),
            DocumentUrl = url
        };
        _db.HostApplications.Add(application);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(application, user.DisplayName, user.Email);
    }

    public async Task<IReadOnlyList<HostApplicationDto>> ListAsync(string adminId, CancellationToken cancellationToken = default)
    {
        await RequireAdminAsync(adminId);
        return await _db.HostApplications
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new HostApplicationDto(
                x.Id,
                x.FullName,
                x.DocumentUrl,
                (int)x.Status,
                x.AdminNote,
                x.CreatedAtUtc,
                x.User!.DisplayName,
                x.User.Email))
            .ToListAsync(cancellationToken);
    }

    public async Task<HostApplicationDto> ApproveAsync(string adminId, Guid id, CancellationToken cancellationToken = default)
    {
        await RequireAdminAsync(adminId);
        var application = await FindAsync(id, cancellationToken);
        if (application.Status != HostApplicationStatus.Pending)
        {
            throw new InvalidOperationException("Цю заявку вже розглянуто.");
        }

        var user = await _users.FindByIdAsync(application.UserId) ?? throw new InvalidOperationException("Користувача не знайдено.");
        user.IsHost = true;
        var update = await _users.UpdateAsync(user);
        if (!update.Succeeded)
        {
            throw new InvalidOperationException(string.Join(" ", update.Errors.Select(e => e.Description)));
        }

        application.Status = HostApplicationStatus.Approved;
        await _db.SaveChangesAsync(cancellationToken);
        return Map(application, user.DisplayName, user.Email);
    }

    public async Task<HostApplicationDto> RejectAsync(string adminId, Guid id, string? note, CancellationToken cancellationToken = default)
    {
        await RequireAdminAsync(adminId);
        var application = await FindAsync(id, cancellationToken);
        if (application.Status != HostApplicationStatus.Pending)
        {
            throw new InvalidOperationException("Цю заявку вже розглянуто.");
        }

        application.Status = HostApplicationStatus.Rejected;
        application.AdminNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        await _db.SaveChangesAsync(cancellationToken);
        var user = await _users.FindByIdAsync(application.UserId);
        return Map(application, user?.DisplayName, user?.Email);
    }

    private async Task RequireAdminAsync(string adminId)
    {
        var admin = await _users.FindByIdAsync(adminId);
        if (admin is null || !admin.IsAdmin || admin.IsBlocked)
        {
            throw new UnauthorizedAccessException("Ця дія доступна лише адміністратору.");
        }
    }

    private async Task<HostApplication> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _db.HostApplications.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Заявку не знайдено.");
    }

    private static HostApplicationDto Map(HostApplication application, string? name, string? email) =>
        new(application.Id, application.FullName, application.DocumentUrl, (int)application.Status, application.AdminNote, application.CreatedAtUtc, name, email);
}
