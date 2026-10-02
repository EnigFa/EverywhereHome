using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Application.Messages;

namespace Project.Api.Controllers;

[ApiController]
[Authorize]
public class MessagesController : ControllerBase
{
    private readonly IConversationService _conversations;

    public MessagesController(IConversationService conversations)
    {
        _conversations = conversations;
    }

    [HttpGet("api/listings/{listingId:guid}/messages")]
    public async Task<IActionResult> ListingMessages(Guid listingId, CancellationToken cancellationToken)
    {
        return await Run(userId => _conversations.ListListingMessagesAsync(userId, listingId, cancellationToken));
    }

    [HttpPost("api/listings/{listingId:guid}/conversation")]
    public async Task<IActionResult> OpenListing(Guid listingId, CancellationToken cancellationToken)
    {
        return await Run(userId => _conversations.OpenListingAsync(userId, listingId, cancellationToken));
    }

    [HttpPost("api/listings/{listingId:guid}/messages")]
    public async Task<IActionResult> PostListing(Guid listingId, [FromBody] PostMessageRequest request, CancellationToken cancellationToken)
    {
        return await Run(userId => _conversations.PostListingMessageAsync(userId, listingId, request.Text, cancellationToken));
    }

    [HttpGet("api/host/conversations")]
    public async Task<IActionResult> HostConversations(CancellationToken cancellationToken)
    {
        return await Run(userId => _conversations.ListHostConversationsAsync(userId, cancellationToken));
    }

    [HttpGet("api/conversations/{id:guid}/messages")]
    public async Task<IActionResult> Conversation(Guid id, CancellationToken cancellationToken)
    {
        return await Run(userId => _conversations.ListConversationAsync(userId, id, cancellationToken));
    }

    [HttpPost("api/conversations/{id:guid}/messages")]
    public async Task<IActionResult> PostConversation(Guid id, [FromBody] PostMessageRequest request, CancellationToken cancellationToken)
    {
        return await Run(userId => _conversations.PostToConversationAsync(userId, id, request.Text, cancellationToken));
    }

    [HttpPut("api/conversations/{id:guid}/messages/{messageId:guid}")]
    public async Task<IActionResult> EditMessage(Guid id, Guid messageId, [FromBody] PostMessageRequest request, CancellationToken cancellationToken)
    {
        return await Run(userId => _conversations.EditMessageAsync(userId, id, messageId, request.Text, cancellationToken));
    }

    [HttpDelete("api/conversations/{id:guid}/messages/{messageId:guid}")]
    public async Task<IActionResult> DeleteMessage(Guid id, Guid messageId, CancellationToken cancellationToken)
    {
        return await Run(userId => _conversations.DeleteMessageAsync(userId, id, messageId, cancellationToken));
    }

    [HttpDelete("api/conversations/{id:guid}/history")]
    public async Task<IActionResult> ClearHistory(Guid id, CancellationToken cancellationToken)
    {
        return await Run(async userId =>
        {
            await _conversations.ClearHistoryAsync(userId, id, cancellationToken);
            return new { ok = true };
        });
    }

    [HttpGet("api/conversations")]
    public async Task<IActionResult> Inbox(CancellationToken cancellationToken)
    {
        return await Run(userId => _conversations.ListInboxAsync(userId, cancellationToken));
    }

    [HttpPost("api/conversations/with/{userId}")]
    public async Task<IActionResult> OpenDirect(string userId, CancellationToken cancellationToken)
    {
        return await Run(me => _conversations.OpenDirectAsync(me, userId, cancellationToken));
    }

    [HttpGet("api/support/messages")]
    public async Task<IActionResult> Support(CancellationToken cancellationToken)
    {
        return await Run(userId => _conversations.ListSupportMessagesAsync(userId, cancellationToken));
    }

    [HttpPost("api/support/messages")]
    public async Task<IActionResult> PostSupport([FromBody] PostMessageRequest request, CancellationToken cancellationToken)
    {
        return await Run(userId => _conversations.PostSupportMessageAsync(userId, request.Text, cancellationToken));
    }

    private async Task<IActionResult> Run<T>(Func<string, Task<T>> action)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await action(userId));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
