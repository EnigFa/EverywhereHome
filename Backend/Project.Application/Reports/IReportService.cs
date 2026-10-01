namespace Project.Application.Reports;

public record CreateReportRequest(int Target, Guid? ListingId, string? ReportedUserId, string Text);

public record ReportDto(
    Guid Id,
    int Target,
    int Status,
    string Text,
    Guid? ListingId,
    string? ListingTitle,
    string? ReportedUserId,
    string? ReportedUserName,
    string ReporterName,
    DateTime CreatedAtUtc);

public interface IReportService
{
    Task<ReportDto> CreateAsync(string reporterId, CreateReportRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReportDto>> ListAsync(string adminId, CancellationToken cancellationToken = default);
    Task<ReportDto> MarkReviewedAsync(string adminId, Guid id, CancellationToken cancellationToken = default);
    Task<ReportDto> UnpublishFromReportAsync(string adminId, Guid id, CancellationToken cancellationToken = default);
    Task<ReportDto> BlockFromReportAsync(string adminId, Guid id, CancellationToken cancellationToken = default);
    Task UnpublishListingAsync(string adminId, Guid listingId, CancellationToken cancellationToken = default);
}
