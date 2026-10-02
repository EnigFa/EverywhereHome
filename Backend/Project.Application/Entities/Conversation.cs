namespace Project.Application.Entities;

public enum ConversationKind
{
    Listing = 0,
    Support = 1,
    Report = 2,
    Direct = 3
}

public enum CaseStatus
{
    New = 0,
    Resolved = 1,
    InProgress = 2
}

public class Conversation
{
    public Guid Id { get; set; }
    public ConversationKind Kind { get; set; }
    public Guid? ListingId { get; set; }
    public Listing? Listing { get; set; }
    public string UserId { get; set; } = string.Empty;
    public AppUser? User { get; set; }
    public string? HostId { get; set; }
    public AppUser? Host { get; set; }
    public Guid? ReportId { get; set; }
    public CaseStatus Status { get; set; } = CaseStatus.New;
    public string? AssigneeId { get; set; }
    public AppUser? Assignee { get; set; }
    public string? ResolvedById { get; set; }
    public AppUser? ResolvedBy { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public string? Decision { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}

public class ConversationRead
{
    public Guid ConversationId { get; set; }
    public Conversation? Conversation { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime LastReadAtUtc { get; set; }
}

public class ChatMessage
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Conversation? Conversation { get; set; }
    public string SenderId { get; set; } = string.Empty;
    public AppUser? Sender { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? EditedAtUtc { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
}
