namespace Project.Application.Entities;

public enum HostApplicationStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}

public class HostApplication
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public AppUser? User { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string DocumentUrl { get; set; } = string.Empty;
    public HostApplicationStatus Status { get; set; } = HostApplicationStatus.Pending;
    public string? AdminNote { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
