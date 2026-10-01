using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Project.Application.Entities;
using Project.Application.Reports;
using Project.Infrastructure.Data;

namespace Project.Infrastructure.Reports;

public class ReportService : IReportService
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _users;

    public ReportService(AppDbContext db, UserManager<AppUser> users)
    {
        _db = db;
        _users = users;
    }

    public async Task<ReportDto> CreateAsync(string reporterId, CreateReportRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            throw new InvalidOperationException("Напишіть текст скарги.");
        }

        var reporter = await _users.FindByIdAsync(reporterId) ?? throw new InvalidOperationException("Користувача не знайдено.");
        var report = new Report
        {
            Id = Guid.NewGuid(),
            ReporterId = reporterId,
            Text = request.Text.Trim()
        };

        if (request.Target == (int)ReportTarget.Listing)
        {
            if (request.ListingId is null)
            {
                throw new InvalidOperationException("Вкажіть оголошення.");
            }

            var listing = await _db.Listings.FirstOrDefaultAsync(l => l.Id == request.ListingId, cancellationToken)
                ?? throw new InvalidOperationException("Оголошення не знайдено.");
            if (listing.HostId == reporterId)
            {
                throw new InvalidOperationException("Не можна скаржитись на власне оголошення.");
            }

            report.Target = ReportTarget.Listing;
            report.ListingId = listing.Id;
            report.ReportedUserId = listing.HostId;
        }
        else if (request.Target == (int)ReportTarget.Host)
        {
            if (string.IsNullOrWhiteSpace(request.ReportedUserId))
            {
                throw new InvalidOperationException("Вкажіть господаря.");
            }

            if (request.ReportedUserId == reporterId)
            {
                throw new InvalidOperationException("Не можна скаржитись на себе.");
            }

            var host = await _users.FindByIdAsync(request.ReportedUserId) ?? throw new InvalidOperationException("Користувача не знайдено.");
            report.Target = ReportTarget.Host;
            report.ReportedUserId = host.Id;
            report.ListingId = request.ListingId;
        }
        else
        {
            throw new InvalidOperationException("Невідомий тип скарги.");
        }

        _db.Reports.Add(report);
        await _db.SaveChangesAsync(cancellationToken);
        return await MapAsync(report.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<ReportDto>> ListAsync(string adminId, CancellationToken cancellationToken = default)
    {
        await RequireAdminAsync(adminId);
        var ids = await _db.Reports.AsNoTracking().OrderByDescending(r => r.CreatedAtUtc).Select(r => r.Id).ToListAsync(cancellationToken);
        var result = new List<ReportDto>();
        foreach (var id in ids)
        {
            result.Add(await MapAsync(id, cancellationToken));
        }

        return result;
    }

    public async Task<ReportDto> MarkReviewedAsync(string adminId, Guid id, CancellationToken cancellationToken = default)
    {
        await RequireAdminAsync(adminId);
        var report = await FindAsync(id, cancellationToken);
        report.Status = ReportStatus.Reviewed;
        await _db.SaveChangesAsync(cancellationToken);
        return await MapAsync(id, cancellationToken);
    }

    public async Task<ReportDto> UnpublishFromReportAsync(string adminId, Guid id, CancellationToken cancellationToken = default)
    {
        await RequireAdminAsync(adminId);
        var report = await FindAsync(id, cancellationToken);
        if (report.ListingId is null)
        {
            throw new InvalidOperationException("У цій скарзі немає оголошення.");
        }

        await UnpublishListingAsync(adminId, report.ListingId.Value, cancellationToken);
        report.Status = ReportStatus.Reviewed;
        await _db.SaveChangesAsync(cancellationToken);
        return await MapAsync(id, cancellationToken);
    }

    public async Task<ReportDto> BlockFromReportAsync(string adminId, Guid id, CancellationToken cancellationToken = default)
    {
        await RequireAdminAsync(adminId);
        var report = await FindAsync(id, cancellationToken);
        if (string.IsNullOrWhiteSpace(report.ReportedUserId))
        {
            throw new InvalidOperationException("У цій скарзі немає користувача.");
        }

        if (report.ReportedUserId == adminId)
        {
            throw new InvalidOperationException("Не можна заблокувати власний акаунт.");
        }

        var user = await _users.FindByIdAsync(report.ReportedUserId) ?? throw new InvalidOperationException("Користувача не знайдено.");
        if (user.IsAdmin)
        {
            throw new InvalidOperationException("Адміністратора заблокувати не можна.");
        }

        user.IsBlocked = true;
        var update = await _users.UpdateAsync(user);
        if (!update.Succeeded)
        {
            throw new InvalidOperationException(string.Join(" ", update.Errors.Select(e => e.Description)));
        }

        report.Status = ReportStatus.Reviewed;
        await _db.SaveChangesAsync(cancellationToken);
        return await MapAsync(id, cancellationToken);
    }

    public async Task UnpublishListingAsync(string adminId, Guid listingId, CancellationToken cancellationToken = default)
    {
        await RequireAdminAsync(adminId);
        var listing = await _db.Listings.FirstOrDefaultAsync(l => l.Id == listingId, cancellationToken)
            ?? throw new InvalidOperationException("Оголошення не знайдено.");
        listing.IsPublished = false;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task RequireAdminAsync(string adminId)
    {
        var admin = await _users.FindByIdAsync(adminId);
        if (admin is null || !admin.IsAdmin || admin.IsBlocked)
        {
            throw new UnauthorizedAccessException("Ця дія доступна лише адміністратору.");
        }
    }

    private async Task<Report> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _db.Reports.FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Скаргу не знайдено.");
    }

    private async Task<ReportDto> MapAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _db.Reports
            .AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new ReportDto(
                r.Id,
                (int)r.Target,
                (int)r.Status,
                r.Text,
                r.ListingId,
                r.Listing != null ? r.Listing.Title : null,
                r.ReportedUserId,
                r.ReportedUser != null ? r.ReportedUser.DisplayName : null,
                r.Reporter!.DisplayName,
                r.CreatedAtUtc))
            .FirstAsync(cancellationToken);
    }
}
