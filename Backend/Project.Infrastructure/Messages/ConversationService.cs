using System.Collections.Concurrent;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Project.Application.Entities;
using Project.Application.Messages;
using Project.Infrastructure.Data;

namespace Project.Infrastructure.Messages;

public class ConversationService : IConversationService
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> ListingLocks = new();
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
        if (conversation is null)
        {
            return [];
        }

        await MarkReadAsync(userId, conversation.Id, cancellationToken);
        return await MessagesAsync(conversation.Id, userId, cancellationToken);
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
        await MarkReadAsync(userId, conversationId, cancellationToken);
        return await MessagesAsync(conversationId, userId, cancellationToken);
    }

    public async Task<MessageDto> PostToConversationAsync(string userId, Guid conversationId, string text, CancellationToken cancellationToken = default)
    {
        await RequireParticipantAsync(userId, conversationId);
        return await AddMessageAsync(conversationId, userId, text, cancellationToken);
    }

    public async Task<IReadOnlyList<MessageDto>> ListSupportMessagesAsync(string userId, CancellationToken cancellationToken = default)
    {
        var conversation = await FindSupportAsync(userId, create: false, cancellationToken);
        if (conversation is null)
        {
            return [];
        }

        await MarkReadAsync(userId, conversation.Id, cancellationToken);
        return await MessagesAsync(conversation.Id, userId, cancellationToken);
    }

    public async Task<MessageDto> PostSupportMessageAsync(string userId, string text, CancellationToken cancellationToken = default)
    {
        var conversation = await FindSupportAsync(userId, create: true, cancellationToken);
        if (conversation!.Status == CaseStatus.Resolved)
        {
            conversation.Status = CaseStatus.New;
            conversation.AssigneeId = null;
            conversation.ResolvedById = null;
            conversation.ResolvedAtUtc = null;
            conversation.Decision = null;
        }

        return await AddMessageAsync(conversation.Id, userId, text, cancellationToken);
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
        return conversation is null ? [] : await MessagesAsync(conversation.Id, adminId, cancellationToken);
    }

    public async Task<MessageDto> PostSupportForAdminAsync(string adminId, string userId, string text, CancellationToken cancellationToken = default)
    {
        await RequireAdminAsync(adminId);
        var conversation = await FindSupportAsync(userId, create: true, cancellationToken);
        return await AddMessageAsync(conversation!.Id, adminId, text, cancellationToken);
    }

    public async Task<IReadOnlyList<ConversationDto>> ListInboxAsync(string userId, CancellationToken cancellationToken = default)
    {
        var items = await _db.Conversations
            .AsNoTracking()
            .Where(c => c.UserId == userId || c.HostId == userId)
            .OrderByDescending(c => c.CreatedAtUtc)
            .Select(c => new ConversationDto(
                c.Id,
                (int)c.Kind,
                c.ListingId,
                c.Listing != null ? c.Listing.Title
                    : c.Kind == ConversationKind.Direct
                        ? (c.UserId == userId ? c.Host!.DisplayName : c.User!.DisplayName)
                        : c.Kind == ConversationKind.Support ? "Підтримка" : "Скарга",
                c.UserId,
                c.User!.DisplayName,
                c.HostId))
            .ToListAsync(cancellationToken);
        return await WithUnreadAsync(userId, items, cancellationToken);
    }

    public async Task<ConversationDto> OpenDirectAsync(string userId, string otherUserId, CancellationToken cancellationToken = default)
    {
        if (userId == otherUserId)
        {
            throw new InvalidOperationException("Не можна написати самому собі.");
        }

        var other = await _users.FindByIdAsync(otherUserId) ?? throw new InvalidOperationException("Користувача не знайдено.");
        if (other.IsBlocked)
        {
            throw new InvalidOperationException("Акаунт заблоковано.");
        }

        var conversation = await _db.Conversations.FirstOrDefaultAsync(c =>
            c.Kind == ConversationKind.Direct &&
            ((c.UserId == userId && c.HostId == otherUserId) || (c.UserId == otherUserId && c.HostId == userId)),
            cancellationToken);
        if (conversation is null)
        {
            conversation = new Conversation
            {
                Id = Guid.NewGuid(),
                Kind = ConversationKind.Direct,
                UserId = userId,
                HostId = otherUserId
            };
            _db.Conversations.Add(conversation);
            await _db.SaveChangesAsync(cancellationToken);
        }

        return (await ListInboxAsync(userId, cancellationToken)).First(c => c.Id == conversation.Id);
    }

    public async Task<ConversationDto> OpenListingAsync(string userId, Guid listingId, CancellationToken cancellationToken = default)
    {
        var conversation = await FindListingConversationAsync(userId, listingId, create: true, cancellationToken)
            ?? throw new InvalidOperationException("Переписку не знайдено.");
        return (await ListInboxAsync(userId, cancellationToken)).First(c => c.Id == conversation.Id);
    }

    public async Task<IReadOnlyList<SupportTicketDto>> ListSupportTicketsAsync(string adminId, int? status, CancellationToken cancellationToken = default)
    {
        await RequireAdminAsync(adminId);
        var query = _db.Conversations.AsNoTracking().Where(c => c.Kind == ConversationKind.Support);
        if (status is not null)
        {
            query = query.Where(c => (int)c.Status == status);
        }

        return await query
            .OrderByDescending(c => c.CreatedAtUtc)
            .Select(c => new SupportTicketDto(
                c.Id,
                c.UserId,
                c.User!.DisplayName,
                (int)c.Status,
                c.AssigneeId,
                c.Assignee != null ? c.Assignee.DisplayName : null,
                c.ResolvedBy != null ? c.ResolvedBy.DisplayName : null,
                c.CreatedAtUtc,
                c.ResolvedAtUtc,
                c.Decision))
            .ToListAsync(cancellationToken);
    }

    public async Task<SupportTicketDto> TakeSupportAsync(string adminId, Guid id, CancellationToken cancellationToken = default)
    {
        await RequireAdminAsync(adminId);
        var conversation = await SupportTicketAsync(id, cancellationToken);
        if (conversation.Status != CaseStatus.New)
        {
            throw new InvalidOperationException("Заявка вже не в черзі.");
        }

        conversation.Status = CaseStatus.InProgress;
        conversation.AssigneeId = adminId;
        await _db.SaveChangesAsync(cancellationToken);
        return (await ListSupportTicketsAsync(adminId, null, cancellationToken)).First(x => x.Id == id);
    }

    public async Task<SupportTicketDto> ReleaseSupportAsync(string adminId, Guid id, CancellationToken cancellationToken = default)
    {
        await RequireAdminAsync(adminId);
        var conversation = await SupportTicketAsync(id, cancellationToken);
        if (conversation.Status != CaseStatus.InProgress)
        {
            throw new InvalidOperationException("Повернути в чергу можна лише заявку в роботі.");
        }

        conversation.Status = CaseStatus.New;
        conversation.AssigneeId = null;
        await _db.SaveChangesAsync(cancellationToken);
        return (await ListSupportTicketsAsync(adminId, null, cancellationToken)).First(x => x.Id == id);
    }

    public async Task<SupportTicketDto> ResolveSupportAsync(string adminId, Guid id, string decision, CancellationToken cancellationToken = default)
    {
        await RequireAdminAsync(adminId);
        if (string.IsNullOrWhiteSpace(decision))
        {
            throw new InvalidOperationException("Напишіть рішення по заявці.");
        }

        var conversation = await SupportTicketAsync(id, cancellationToken);
        if (conversation.Status != CaseStatus.InProgress)
        {
            throw new InvalidOperationException("Спочатку візьміть заявку в роботу.");
        }

        conversation.Status = CaseStatus.Resolved;
        conversation.ResolvedById = adminId;
        conversation.ResolvedAtUtc = DateTime.UtcNow;
        conversation.Decision = decision.Trim();
        await _db.SaveChangesAsync(cancellationToken);
        return (await ListSupportTicketsAsync(adminId, null, cancellationToken)).First(x => x.Id == id);
    }

    private async Task<Conversation> SupportTicketAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _db.Conversations.FirstOrDefaultAsync(c => c.Id == id && c.Kind == ConversationKind.Support, cancellationToken)
            ?? throw new InvalidOperationException("Заявку підтримки не знайдено.");
    }

    private async Task<IReadOnlyList<ConversationDto>> WithUnreadAsync(string userId, List<ConversationDto> items, CancellationToken cancellationToken)
    {
        if (items.Count == 0)
        {
            return items;
        }

        var ids = items.Select(x => x.Id).ToList();
        var reads = await _db.ConversationReads.AsNoTracking()
            .Where(r => r.UserId == userId && ids.Contains(r.ConversationId))
            .ToListAsync(cancellationToken);
            var messages = await _db.ChatMessages.AsNoTracking()
            .Where(m => ids.Contains(m.ConversationId) && m.SenderId != userId && m.DeletedAtUtc == null)
            .Select(m => new { m.ConversationId, m.CreatedAtUtc })
            .ToListAsync(cancellationToken);
        return items.Select(item =>
        {
            var readAt = reads.FirstOrDefault(r => r.ConversationId == item.Id)?.LastReadAtUtc ?? DateTime.MinValue;
            var unread = messages.Count(m => m.ConversationId == item.Id && m.CreatedAtUtc > readAt);
            return item with { UnreadCount = unread };
        }).ToList();
    }

    private async Task MarkReadAsync(string userId, Guid conversationId, CancellationToken cancellationToken)
    {
        var read = await _db.ConversationReads.FirstOrDefaultAsync(
            r => r.ConversationId == conversationId && r.UserId == userId, cancellationToken);
        if (read is null)
        {
            _db.ConversationReads.Add(new ConversationRead
            {
                ConversationId = conversationId,
                UserId = userId,
                LastReadAtUtc = DateTime.UtcNow
            });
        }
        else
        {
            read.LastReadAtUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
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

        var gate = ListingLocks.GetOrAdd($"{guestId}:{listingId}", _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
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
        finally
        {
            gate.Release();
        }
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
        return MapMessage(message, sender.DisplayName);
    }

    public async Task<MessageDto> EditMessageAsync(string userId, Guid conversationId, Guid messageId, string text, CancellationToken cancellationToken = default)
    {
        var message = await OwnMessageAsync(userId, conversationId, messageId, cancellationToken);
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("Порожнє повідомлення не надсилається.");
        }

        message.Text = text.Trim();
        message.EditedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        var sender = await _users.FindByIdAsync(message.SenderId);
        return MapMessage(message, sender?.DisplayName ?? "");
    }

    public async Task<MessageDto> DeleteMessageAsync(string userId, Guid conversationId, Guid messageId, CancellationToken cancellationToken = default)
    {
        var message = await OwnMessageAsync(userId, conversationId, messageId, cancellationToken);
        message.Text = "";
        message.DeletedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        var sender = await _users.FindByIdAsync(message.SenderId);
        return MapMessage(message, sender?.DisplayName ?? "");
    }

    public async Task ClearHistoryAsync(string userId, Guid conversationId, CancellationToken cancellationToken = default)
    {
        await RequireParticipantAsync(userId, conversationId);
        var conversation = await _db.Conversations.FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken)
            ?? throw new InvalidOperationException("Переписку не знайдено.");
        if (conversation.Kind == ConversationKind.Report)
        {
            throw new InvalidOperationException("Історію скарги видалити не можна.");
        }

        var messages = await _db.ChatMessages.Where(m => m.ConversationId == conversationId).ToListAsync(cancellationToken);
        _db.ChatMessages.RemoveRange(messages);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<ChatMessage> OwnMessageAsync(string userId, Guid conversationId, Guid messageId, CancellationToken cancellationToken)
    {
        await RequireParticipantAsync(userId, conversationId);
        var message = await _db.ChatMessages.FirstOrDefaultAsync(m => m.Id == messageId && m.ConversationId == conversationId, cancellationToken)
            ?? throw new InvalidOperationException("Повідомлення не знайдено.");
        if (message.SenderId != userId)
        {
            throw new UnauthorizedAccessException("Можна змінювати лише свої повідомлення.");
        }

        if (message.DeletedAtUtc is not null)
        {
            throw new InvalidOperationException("Видалене повідомлення змінити не можна.");
        }

        return message;
    }

    private static MessageDto MapMessage(ChatMessage message, string senderName)
    {
        var deleted = message.DeletedAtUtc is not null;
        return new MessageDto(message.Id, message.SenderId, senderName, deleted ? "" : message.Text, message.CreatedAtUtc, message.EditedAtUtc, deleted);
    }

    private async Task<IReadOnlyList<MessageDto>> MessagesAsync(Guid conversationId, string viewerId, CancellationToken cancellationToken)
    {
        var messages = await _db.ChatMessages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.CreatedAtUtc)
            .Select(m => new MessageDto(
                m.Id,
                m.SenderId,
                m.Sender!.DisplayName,
                m.DeletedAtUtc == null ? m.Text : "",
                m.CreatedAtUtc,
                m.EditedAtUtc,
                m.DeletedAtUtc != null))
            .ToListAsync(cancellationToken);
        var otherReads = await _db.ConversationReads.AsNoTracking()
            .Where(r => r.ConversationId == conversationId && r.UserId != viewerId)
            .Select(r => r.LastReadAtUtc)
            .ToListAsync(cancellationToken);
        if (otherReads.Count == 0)
        {
            return messages;
        }

        return messages.Select(message =>
            message.SenderId == viewerId && otherReads.Any(read => read >= message.CreatedAtUtc)
                ? message with { ReadByOther = true }
                : message).ToList();
    }

    private async Task RequireParticipantAsync(string userId, Guid conversationId)
    {
        var conversation = await _db.Conversations.AsNoTracking().FirstOrDefaultAsync(c => c.Id == conversationId)
            ?? throw new InvalidOperationException("Переписку не знайдено.");
        var user = await _users.FindByIdAsync(userId) ?? throw new InvalidOperationException("Користувача не знайдено.");
        var allowed = conversation.UserId == userId
            || conversation.HostId == userId
            || (conversation.Kind is ConversationKind.Support or ConversationKind.Report && (user.IsAdmin || user.IsChiefAdmin));
        if (!allowed)
        {
            throw new UnauthorizedAccessException("Ця переписка вам недоступна.");
        }
    }

    private async Task RequireAdminAsync(string adminId)
    {
        var admin = await _users.FindByIdAsync(adminId);
        if (admin is null || (!admin.IsAdmin && !admin.IsChiefAdmin) || admin.IsBlocked)
        {
            throw new UnauthorizedAccessException("Ця дія доступна лише адміністратору.");
        }
    }
}
