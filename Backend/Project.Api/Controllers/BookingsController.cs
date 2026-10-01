using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Application.Bookings;

namespace Project.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/bookings")]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookings;

    public BookingsController(IBookingService bookings)
    {
        _bookings = bookings;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BookingDto>>> Mine(CancellationToken cancellationToken)
    {
        var userId = UserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        return Ok(await _bookings.ListMineAsync(userId, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BookingDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var userId = UserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var booking = await _bookings.GetAsync(userId, id, cancellationToken);
        return booking is null ? NotFound() : Ok(booking);
    }

    [HttpPost]
    public async Task<ActionResult<BookingDto>> Create([FromBody] CreateBookingRequest request, CancellationToken cancellationToken)
    {
        var userId = UserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        try
        {
            var booking = await _bookings.CreateAsync(userId, request, cancellationToken);
            return CreatedAtAction(nameof(Get), new { id = booking.Id }, booking);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/pay-stub")]
    public async Task<ActionResult<BookingDto>> PayStub(Guid id, [FromBody] ConfirmPaymentRequest request, CancellationToken cancellationToken)
    {
        var userId = UserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await _bookings.ConfirmStubPaymentAsync(userId, id, request, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<BookingDto>> Cancel(Guid id, CancellationToken cancellationToken)
    {
        return await ChangeStatus(id, _bookings.CancelAsync, cancellationToken);
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<ActionResult<BookingDto>> Complete(Guid id, CancellationToken cancellationToken)
    {
        return await ChangeStatus(id, _bookings.CompleteAsync, cancellationToken);
    }

    private async Task<ActionResult<BookingDto>> ChangeStatus(
        Guid id,
        Func<string, Guid, CancellationToken, Task<BookingDto>> action,
        CancellationToken cancellationToken)
    {
        var userId = UserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await action(userId, id, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private string? UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);
}
