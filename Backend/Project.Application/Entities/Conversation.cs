namespace Project.Application.Entities;

public enum ConversationKind
{
    Listing = 0,
    Support = 1
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
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
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
}
