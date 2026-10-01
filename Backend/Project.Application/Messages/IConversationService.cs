namespace Project.Application.Messages;

public record MessageDto(Guid Id, string SenderId, string SenderName, string Text, DateTime CreatedAtUtc);

public record ConversationDto(Guid Id, int Kind, Guid? ListingId, string? ListingTitle, string UserId, string UserName, string? HostId);

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
}
