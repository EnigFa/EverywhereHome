using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Project.Application.Entities;
using Project.Application.Messages;
using Project.Infrastructure.Data;

namespace Project.Infrastructure.Messages;

public class ConversationService : IConversationService
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _users;

    public ConversationService(AppDbContext db, UserManager<AppUser> users)
    {
        _db = db;
        _users = users;
    }

    public async Task<IReadOnlyList<MessageDto>> ListListingMessagesAsync(string userId, Guid listingId, CancellationToken cancellationToken = default)
    {
        var conversation = await FindListingConversationAsync(userId, listingId, create: false, cancellationToken);
        return conversation is null ? [] : await MessagesAsync(conversation.Id, cancellationToken);
    }

    public async Task<MessageDto> PostListingMessageAsync(string userId, Guid listingId, string text, CancellationToken cancellationToken = default)
    {
        var conversation = await FindListingConversationAsync(userId, listingId, create: true, cancellationToken)
            ?? throw new InvalidOperationException("Переписку не знайдено.");
        return await AddMessageAsync(conversation.Id, userId, text, cancellationToken);
    }

    public async Task<IReadOnlyList<ConversationDto>> ListHostConversationsAsync(string hostId, CancellationToken cancellationToken = default)
    {
        return await _db.Conversations
            .AsNoTracking()
            .Where(c => c.Kind == ConversationKind.Listing && c.HostId == hostId)
            .OrderByDescending(c => c.CreatedAtUtc)
            .Select(c => new ConversationDto(c.Id, (int)c.Kind, c.ListingId, c.Listing!.Title, c.UserId, c.User!.DisplayName, c.HostId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MessageDto>> ListConversationAsync(string userId, Guid conversationId, CancellationToken cancellationToken = default)
    {
        await RequireParticipantAsync(userId, conversationId);
        return await MessagesAsync(conversationId, cancellationToken);
    }

    public async Task<MessageDto> PostToConversationAsync(string userId, Guid conversationId, string text, CancellationToken cancellationToken = default)
    {
        await RequireParticipantAsync(userId, conversationId);
        return await AddMessageAsync(conversationId, userId, text, cancellationToken);
    }

    public async Task<IReadOnlyList<MessageDto>> ListSupportMessagesAsync(string userId, CancellationToken cancellationToken = default)
    {
        var conversation = await FindSupportAsync(userId, create: false, cancellationToken);
        return conversation is null ? [] : await MessagesAsync(conversation.Id, cancellationToken);
    }

    public async Task<MessageDto> PostSupportMessageAsync(string userId, string text, CancellationToken cancellationToken = default)
    {
        var conversation = await FindSupportAsync(userId, create: true, cancellationToken);
        return await AddMessageAsync(conversation!.Id, userId, text, cancellationToken);
    }

    public async Task<IReadOnlyList<ConversationDto>> ListSupportForAdminAsync(string adminId, CancellationToken cancellationToken = default)
    {
        await RequireAdminAsync(adminId);
        return await _db.Conversations
            .AsNoTracking()
            .Where(c => c.Kind == ConversationKind.Support)
            .OrderByDescending(c => c.CreatedAtUtc)
            .Select(c => new ConversationDto(c.Id, (int)c.Kind, null, null, c.UserId, c.User!.DisplayName, null))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MessageDto>> ListSupportForAdminUserAsync(string adminId, string userId, CancellationToken cancellationToken = default)
    {
        await RequireAdminAsync(adminId);
        var conversation = await FindSupportAsync(userId, create: false, cancellationToken);
        return conversation is null ? [] : await MessagesAsync(conversation.Id, cancellationToken);
    }

    public async Task<MessageDto> PostSupportForAdminAsync(string adminId, string userId, string text, CancellationToken cancellationToken = default)
    {
        await RequireAdminAsync(adminId);
        var conversation = await FindSupportAsync(userId, create: true, cancellationToken);
        return await AddMessageAsync(conversation!.Id, adminId, text, cancellationToken);
    }

    private async Task<Conversation?> FindListingConversationAsync(string userId, Guid listingId, bool create, CancellationToken cancellationToken)
    {
        var listing = await _db.Listings.FirstOrDefaultAsync(l => l.Id == listingId && l.IsPublished && !l.Host!.IsBlocked, cancellationToken)
            ?? throw new InvalidOperationException("Оголошення не знайдено.");
        var guestId = userId == listing.HostId ? null : userId;
        if (guestId is null)
        {
            throw new InvalidOperationException("Господар відповідає в уже відкритій переписці.");
        }

        var conversation = await _db.Conversations.FirstOrDefaultAsync(
            c => c.Kind == ConversationKind.Listing && c.ListingId == listingId && c.UserId == guestId,
            cancellationToken);
        if (conversation is not null || !create)
        {
            return conversation;
        }

        conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            Kind = ConversationKind.Listing,
            ListingId = listingId,
            UserId = guestId,
            HostId = listing.HostId
        };
        _db.Conversations.Add(conversation);
        await _db.SaveChangesAsync(cancellationToken);
        return conversation;
    }

    private async Task<Conversation?> FindSupportAsync(string userId, bool create, CancellationToken cancellationToken)
    {
        var conversation = await _db.Conversations.FirstOrDefaultAsync(
            c => c.Kind == ConversationKind.Support && c.UserId == userId,
            cancellationToken);
        if (conversation is not null || !create)
        {
            return conversation;
        }

        conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            Kind = ConversationKind.Support,
            UserId = userId
        };
        _db.Conversations.Add(conversation);
        await _db.SaveChangesAsync(cancellationToken);
        return conversation;
    }

    private async Task<MessageDto> AddMessageAsync(Guid conversationId, string senderId, string text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("Порожнє повідомлення не надсилається.");
        }

        var sender = await _users.FindByIdAsync(senderId) ?? throw new InvalidOperationException("Користувача не знайдено.");
        if (sender.IsBlocked)
        {
            throw new InvalidOperationException("Акаунт заблоковано.");
        }

        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            SenderId = senderId,
            Text = text.Trim()
        };
        _db.ChatMessages.Add(message);
        await _db.SaveChangesAsync(cancellationToken);
        return new MessageDto(message.Id, sender.Id, sender.DisplayName, message.Text, message.CreatedAtUtc);
    }

    private async Task<IReadOnlyList<MessageDto>> MessagesAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        return await _db.ChatMessages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.CreatedAtUtc)
            .Select(m => new MessageDto(m.Id, m.SenderId, m.Sender!.DisplayName, m.Text, m.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    private async Task RequireParticipantAsync(string userId, Guid conversationId)
    {
        var conversation = await _db.Conversations.AsNoTracking().FirstOrDefaultAsync(c => c.Id == conversationId)
            ?? throw new InvalidOperationException("Переписку не знайдено.");
        var user = await _users.FindByIdAsync(userId) ?? throw new InvalidOperationException("Користувача не знайдено.");
        var allowed = conversation.UserId == userId
            || conversation.HostId == userId
            || (conversation.Kind == ConversationKind.Support && user.IsAdmin);
        if (!allowed)
        {
            throw new UnauthorizedAccessException("Ця переписка вам недоступна.");
        }
    }

    private async Task RequireAdminAsync(string adminId)
    {
        var admin = await _users.FindByIdAsync(adminId);
        if (admin is null || !admin.IsAdmin || admin.IsBlocked)
        {
            throw new UnauthorizedAccessException("Ця дія доступна лише адміністратору.");
        }
    }
}
