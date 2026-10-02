namespace Project.Application.HostApplications;

public record HostApplicationDto(
    Guid Id,
    string FullName,
    string DocumentUrl,
    int Status,
    string? AdminNote,
    DateTime CreatedAtUtc,
    string? ApplicantName,
    string? ApplicantEmail,
    string UserId);

public interface IHostApplicationService
{
    Task<HostApplicationDto?> GetMineAsync(string userId, CancellationToken cancellationToken = default);
    Task<HostApplicationDto> SubmitAsync(string userId, string fullName, Stream document, string fileName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HostApplicationDto>> ListAsync(string adminId, CancellationToken cancellationToken = default);
    Task<HostApplicationDto> ApproveAsync(string adminId, Guid id, CancellationToken cancellationToken = default);
    Task<HostApplicationDto> RejectAsync(string adminId, Guid id, string? note, CancellationToken cancellationToken = default);
}
