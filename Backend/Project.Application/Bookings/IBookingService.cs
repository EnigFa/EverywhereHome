using Project.Application.Entities;

namespace Project.Application.Bookings;

public record CreateBookingRequest(
    Guid ListingId,
    DateOnly CheckIn,
    DateOnly CheckOut,
    int GuestsCount,
    PaymentPlan PaymentPlan,
    string? MessageToHost);

public record ConfirmPaymentRequest(PaymentMethod Method);

public record BookingDto(
    Guid Id,
    Guid ListingId,
    string ListingTitle,
    string City,
    DateOnly CheckIn,
    DateOnly CheckOut,
    int GuestsCount,
    PaymentPlan PaymentPlan,
    decimal TotalAmount,
    BookingStatus Status,
    string? MessageToHost);

public interface IBookingService
{
    Task<BookingDto> CreateAsync(string guestId, CreateBookingRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BookingDto>> ListMineAsync(string guestId, CancellationToken cancellationToken = default);
    Task<BookingDto?> GetAsync(string guestId, Guid bookingId, CancellationToken cancellationToken = default);
    Task<BookingDto> ConfirmStubPaymentAsync(string guestId, Guid bookingId, ConfirmPaymentRequest request, CancellationToken cancellationToken = default);
}
