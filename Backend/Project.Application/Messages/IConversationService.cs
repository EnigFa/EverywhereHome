namespace Project.Application.Messages;

public record MessageDto(Guid Id, string SenderId, string SenderName, string Text, DateTime CreatedAtUtc, DateTime? EditedAtUtc = null, bool IsDeleted = false, bool ReadByOther = false);

public record ConversationDto(Guid Id, int Kind, Guid? ListingId, string? ListingTitle, string UserId, string UserName, string? HostId, int UnreadCount = 0);

public record SupportTicketDto(
    Guid Id,
    string UserId,
    string UserName,
    int Status,
    string? AssigneeId,
    string? AssigneeName,
    string? ResolvedByName,
    DateTime CreatedAtUtc,
    DateTime? ResolvedAtUtc,
    string? Decision);

public record PostMessageRequest(string Text);

public interface IConversationService
{
    Task<IReadOnlyList<MessageDto>> ListListingMessagesAsync(string userId, Guid listingId, CancellationToken cancellationToken = default);
    Task<MessageDto> PostListingMessageAsync(string userId, Guid listingId, string text, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConversationDto>> ListHostConversationsAsync(string hostId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MessageDto>> ListConversationAsync(string userId, Guid conversationId, CancellationToken cancellationToken = default);
    Task<MessageDto> PostToConversationAsync(string userId, Guid conversationId, string text, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MessageDto>> ListSupportMessagesAsync(string userId, CancellationToken cancellationToken = default);
    Task<MessageDto> PostSupportMessageAsync(string userId, string text, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConversationDto>> ListSupportForAdminAsync(string adminId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MessageDto>> ListSupportForAdminUserAsync(string adminId, string userId, CancellationToken cancellationToken = default);
    Task<MessageDto> PostSupportForAdminAsync(string adminId, string userId, string text, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConversationDto>> ListInboxAsync(string userId, CancellationToken cancellationToken = default);
    Task<ConversationDto> OpenDirectAsync(string userId, string otherUserId, CancellationToken cancellationToken = default);
    Task<ConversationDto> OpenListingAsync(string userId, Guid listingId, CancellationToken cancellationToken = default);
    Task<MessageDto> EditMessageAsync(string userId, Guid conversationId, Guid messageId, string text, CancellationToken cancellationToken = default);
    Task<MessageDto> DeleteMessageAsync(string userId, Guid conversationId, Guid messageId, CancellationToken cancellationToken = default);
    Task ClearHistoryAsync(string userId, Guid conversationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SupportTicketDto>> ListSupportTicketsAsync(string adminId, int? status, CancellationToken cancellationToken = default);
    Task<SupportTicketDto> TakeSupportAsync(string adminId, Guid id, CancellationToken cancellationToken = default);
    Task<SupportTicketDto> ReleaseSupportAsync(string adminId, Guid id, CancellationToken cancellationToken = default);
    Task<SupportTicketDto> ResolveSupportAsync(string adminId, Guid id, string decision, CancellationToken cancellationToken = default);
}
