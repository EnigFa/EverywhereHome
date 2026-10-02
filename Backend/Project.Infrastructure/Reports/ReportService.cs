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

    public async Task<IReadOnlyList<ReportDto>> ListAsync(string adminId, int? status, CancellationToken cancellationToken = default)
    {
        await RequireAdminAsync(adminId);
        var query = _db.Reports.AsNoTracking().AsQueryable();
        if (status is not null)
        {
            query = query.Where(r => (int)r.Status == status);
        }

        var ids = await query.OrderByDescending(r => r.CreatedAtUtc).Select(r => r.Id).ToListAsync(cancellationToken);
        var result = new List<ReportDto>();
        foreach (var id in ids)
        {
            result.Add(await MapAsync(id, cancellationToken));
        }

        return result;
    }

    public async Task<ReportDto> GetAsync(string adminId, Guid id, CancellationToken cancellationToken = default)
    {
        await RequireAdminAsync(adminId);
        return await MapAsync(id, cancellationToken);
    }

    public async Task<ReportDto> TakeAsync(string adminId, Guid id, CancellationToken cancellationToken = default)
    {
        await RequireAdminAsync(adminId);
        var report = await FindAsync(id, cancellationToken);
        if (report.Status != ReportStatus.New)
        {
            throw new InvalidOperationException("Скарга вже не в черзі.");
        }

        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            Kind = ConversationKind.Report,
            UserId = report.ReporterId,
            ListingId = report.ListingId,
            ReportId = report.Id,
            Status = CaseStatus.InProgress,
            AssigneeId = adminId
        };
        _db.Conversations.Add(conversation);
        report.Status = ReportStatus.InProgress;
        report.AssigneeId = adminId;
        report.ConversationId = conversation.Id;
        await _db.SaveChangesAsync(cancellationToken);
        return await MapAsync(id, cancellationToken);
    }

    public async Task<ReportDto> ReleaseAsync(string adminId, Guid id, CancellationToken cancellationToken = default)
    {
        await RequireAdminAsync(adminId);
        var report = await FindAsync(id, cancellationToken);
        if (report.Status != ReportStatus.InProgress)
        {
            throw new InvalidOperationException("Повернути в чергу можна лише скаргу в роботі.");
        }

        report.Status = ReportStatus.New;
        report.AssigneeId = null;
        await _db.SaveChangesAsync(cancellationToken);
        return await MapAsync(id, cancellationToken);
    }

    public async Task<ReportDto> ResolveAsync(string adminId, Guid id, ResolveReportRequest request, CancellationToken cancellationToken = default)
    {
        await RequireAdminAsync(adminId);
        if (string.IsNullOrWhiteSpace(request.Decision))
        {
            throw new InvalidOperationException("Напишіть рішення по скарзі.");
        }

        var report = await FindAsync(id, cancellationToken);
        if (report.Status != ReportStatus.InProgress)
        {
            throw new InvalidOperationException("Спочатку візьміть скаргу в роботу.");
        }

        if (request.Unpublish)
        {
            if (report.ListingId is null)
            {
                throw new InvalidOperationException("У цій скарзі немає оголошення.");
            }

            var listing = await _db.Listings.FirstOrDefaultAsync(l => l.Id == report.ListingId, cancellationToken)
                ?? throw new InvalidOperationException("Оголошення не знайдено.");
            listing.IsPublished = false;
        }

        if (request.Block && !string.IsNullOrWhiteSpace(report.ReportedUserId))
        {
            var user = await _users.FindByIdAsync(report.ReportedUserId) ?? throw new InvalidOperationException("Користувача не знайдено.");
            if (user.IsAdmin || user.Id == adminId)
            {
                throw new InvalidOperationException("Цього користувача заблокувати не можна.");
            }

            user.IsBlocked = true;
            var listings = await _db.Listings.Where(l => l.HostId == user.Id && l.IsPublished).ToListAsync(cancellationToken);
            foreach (var listing in listings)
            {
                listing.IsPublished = false;
            }
        }

        report.Status = ReportStatus.Resolved;
        report.ResolvedById = adminId;
        report.ResolvedAtUtc = DateTime.UtcNow;
        report.Decision = request.Decision.Trim();
        await _db.SaveChangesAsync(cancellationToken);
        return await MapAsync(id, cancellationToken);
    }

    public async Task<ReportDto> MarkReviewedAsync(string adminId, Guid id, CancellationToken cancellationToken = default)
    {
        await RequireAdminAsync(adminId);
        var report = await FindAsync(id, cancellationToken);
        report.Status = ReportStatus.Resolved;
        report.ResolvedById = adminId;
        report.ResolvedAtUtc = DateTime.UtcNow;
        report.Decision ??= "Перевірено";
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
        report.Status = ReportStatus.Resolved;
        report.ResolvedById = adminId;
        report.ResolvedAtUtc = DateTime.UtcNow;
        report.Decision ??= "Перевірено";
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
        var listings = await _db.Listings
            .Where(l => l.HostId == user.Id && l.IsPublished)
            .ToListAsync(cancellationToken);
        foreach (var listing in listings)
        {
            listing.IsPublished = false;
        }

        var update = await _users.UpdateAsync(user);
        if (!update.Succeeded)
        {
            throw new InvalidOperationException(string.Join(" ", update.Errors.Select(e => e.Description)));
        }

        report.Status = ReportStatus.Resolved;
        report.ResolvedById = adminId;
        report.ResolvedAtUtc = DateTime.UtcNow;
        report.Decision ??= "Перевірено";
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
        if (admin is null || (!admin.IsAdmin && !admin.IsChiefAdmin) || admin.IsBlocked)
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
                r.ReporterId,
                r.Reporter!.DisplayName,
                r.CreatedAtUtc,
                r.AssigneeId,
                r.Assignee != null ? r.Assignee.DisplayName : null,
                r.ResolvedById,
                r.ResolvedBy != null ? r.ResolvedBy.DisplayName : null,
                r.ResolvedAtUtc,
                r.Decision,
                r.ConversationId))
            .FirstAsync(cancellationToken);
    }
}
