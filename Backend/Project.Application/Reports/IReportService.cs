namespace Project.Application.Reports;

public record CreateReportRequest(int Target, Guid? ListingId, string? ReportedUserId, string Text);

public record ResolveReportRequest(string Decision, bool Unpublish, bool Block);

public record ReportDto(
    Guid Id,
    int Target,
    int Status,
    string Text,
    Guid? ListingId,
    string? ListingTitle,
    string? ReportedUserId,
    string? ReportedUserName,
    string ReporterId,
    string ReporterName,
    DateTime CreatedAtUtc,
    string? AssigneeId,
    string? AssigneeName,
    string? ResolvedById,
    string? ResolvedByName,
    DateTime? ResolvedAtUtc,
    string? Decision,
    Guid? ConversationId);

public interface IReportService
{
    Task<ReportDto> CreateAsync(string reporterId, CreateReportRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReportDto>> ListAsync(string adminId, int? status, CancellationToken cancellationToken = default);
    Task<ReportDto> GetAsync(string adminId, Guid id, CancellationToken cancellationToken = default);
    Task<ReportDto> TakeAsync(string adminId, Guid id, CancellationToken cancellationToken = default);
    Task<ReportDto> ReleaseAsync(string adminId, Guid id, CancellationToken cancellationToken = default);
    Task<ReportDto> ResolveAsync(string adminId, Guid id, ResolveReportRequest request, CancellationToken cancellationToken = default);
    Task<ReportDto> MarkReviewedAsync(string adminId, Guid id, CancellationToken cancellationToken = default);
    Task<ReportDto> UnpublishFromReportAsync(string adminId, Guid id, CancellationToken cancellationToken = default);
    Task<ReportDto> BlockFromReportAsync(string adminId, Guid id, CancellationToken cancellationToken = default);
    Task UnpublishListingAsync(string adminId, Guid listingId, CancellationToken cancellationToken = default);
}
