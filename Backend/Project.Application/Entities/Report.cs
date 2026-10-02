namespace Project.Application.Entities;

public enum ReportTarget
{
    Listing = 0,
    Host = 1
}

public enum ReportStatus
{
    New = 0,
    Resolved = 1,
    InProgress = 2
}

public class Report
{
    public Guid Id { get; set; }
    public string ReporterId { get; set; } = string.Empty;
    public AppUser? Reporter { get; set; }
    public ReportTarget Target { get; set; }
    public Guid? ListingId { get; set; }
    public Listing? Listing { get; set; }
    public string? ReportedUserId { get; set; }
    public AppUser? ReportedUser { get; set; }
    public string Text { get; set; } = string.Empty;
    public ReportStatus Status { get; set; } = ReportStatus.New;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string? AssigneeId { get; set; }
    public AppUser? Assignee { get; set; }
    public string? ResolvedById { get; set; }
    public AppUser? ResolvedBy { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public string? Decision { get; set; }
    public Guid? ConversationId { get; set; }
}
