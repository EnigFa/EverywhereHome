using Microsoft.EntityFrameworkCore;
using Project.Application.Bookings;
using Project.Application.Entities;
using Project.Infrastructure.Data;

namespace Project.Infrastructure.Bookings;

public class BookingService : IBookingService
{
    private readonly AppDbContext _db;

    public BookingService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<BookingDto> CreateAsync(string guestId, CreateBookingRequest request, CancellationToken cancellationToken = default)
    {
        if (request.CheckOut <= request.CheckIn)
        {
            throw new InvalidOperationException("Дата виїзду має бути пізніше за прибуття.");
        }

        if (request.GuestsCount < 1)
        {
            throw new InvalidOperationException("Потрібен хоча б один гість.");
        }

        var listing = await _db.Listings
            .FirstOrDefaultAsync(l => l.Id == request.ListingId && l.IsPublished, cancellationToken)
            ?? throw new InvalidOperationException("Оголошення не знайдено.");

        if (listing.HostId == guestId)
        {
            throw new InvalidOperationException("Не можна бронювати власне житло.");
        }

        if (request.GuestsCount > listing.MaxGuests)
        {
            throw new InvalidOperationException($"Максимум гостей: {listing.MaxGuests}.");
        }

        var overlap = await _db.Bookings.AnyAsync(b =>
            b.ListingId == listing.Id &&
            b.Status == BookingStatus.Confirmed &&
            b.CheckIn < request.CheckOut &&
            b.CheckOut > request.CheckIn, cancellationToken);

        if (overlap)
        {
            throw new InvalidOperationException("Ці дати вже зайняті.");
        }

        var nights = request.CheckOut.DayNumber - request.CheckIn.DayNumber;
        var total = listing.PricePerNight * nights + listing.CleaningFee;

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            ListingId = listing.Id,
            GuestId = guestId,
            CheckIn = request.CheckIn,
            CheckOut = request.CheckOut,
            GuestsCount = request.GuestsCount,
            PaymentPlan = request.PaymentPlan,
            TotalAmount = total,
            Status = BookingStatus.PendingPayment,
            MessageToHost = request.MessageToHost
        };

        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync(cancellationToken);
        return await MapAsync(booking.Id, cancellationToken) ?? throw new InvalidOperationException("Не вдалося створити бронювання.");
    }

    public async Task<IReadOnlyList<BookingDto>> ListMineAsync(string guestId, CancellationToken cancellationToken = default)
    {
        return await _db.Bookings
            .AsNoTracking()
            .Where(b => b.GuestId == guestId)
            .OrderByDescending(b => b.CreatedAtUtc)
            .Select(b => new BookingDto(
                b.Id,
                b.ListingId,
                b.Listing!.Title,
                b.Listing.City,
                b.CheckIn,
                b.CheckOut,
                b.GuestsCount,
                b.PaymentPlan,
                b.TotalAmount,
                b.Status,
                b.MessageToHost))
            .ToListAsync(cancellationToken);
    }

    public async Task<BookingDto?> GetAsync(string guestId, Guid bookingId, CancellationToken cancellationToken = default)
    {
        return await _db.Bookings
            .AsNoTracking()
            .Where(b => b.Id == bookingId && b.GuestId == guestId)
            .Select(b => new BookingDto(
                b.Id,
                b.ListingId,
                b.Listing!.Title,
                b.Listing.City,
                b.CheckIn,
                b.CheckOut,
                b.GuestsCount,
                b.PaymentPlan,
                b.TotalAmount,
                b.Status,
                b.MessageToHost))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<BookingDto> ConfirmStubPaymentAsync(
        string guestId,
        Guid bookingId,
        ConfirmPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var booking = await _db.Bookings
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.GuestId == guestId, cancellationToken)
            ?? throw new InvalidOperationException("Бронювання не знайдено.");

        if (booking.Status == BookingStatus.Confirmed)
        {
            return await MapAsync(booking.Id, cancellationToken) ?? throw new InvalidOperationException("Бронювання не знайдено.");
        }

        if (booking.Status != BookingStatus.PendingPayment)
        {
            throw new InvalidOperationException("Це бронювання не можна оплатити.");
        }

        var stillFree = !await _db.Bookings.AnyAsync(b =>
            b.Id != booking.Id &&
            b.ListingId == booking.ListingId &&
            b.Status == BookingStatus.Confirmed &&
            b.CheckIn < booking.CheckOut &&
            b.CheckOut > booking.CheckIn, cancellationToken);

        if (!stillFree)
        {
            throw new InvalidOperationException("Ці дати вже зайняті.");
        }

        booking.Status = BookingStatus.Confirmed;
        _db.Payments.Add(new Payment
        {
            Id = Guid.NewGuid(),
            BookingId = booking.Id,
            Amount = booking.TotalAmount,
            Method = request.Method,
            Status = PaymentStatus.StubSucceeded
        });

        await _db.SaveChangesAsync(cancellationToken);
        return await MapAsync(booking.Id, cancellationToken) ?? throw new InvalidOperationException("Бронювання не знайдено.");
    }

    public async Task<BookingDto> CancelAsync(string guestId, Guid bookingId, CancellationToken cancellationToken = default)
    {
        var booking = await _db.Bookings
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.GuestId == guestId, cancellationToken)
            ?? throw new InvalidOperationException("Бронювання не знайдено.");

        if (booking.Status is not (BookingStatus.PendingPayment or BookingStatus.Confirmed))
        {
            throw new InvalidOperationException("Це бронювання не можна скасувати.");
        }

        booking.Status = BookingStatus.Cancelled;
        await _db.SaveChangesAsync(cancellationToken);
        return await MapAsync(booking.Id, cancellationToken) ?? throw new InvalidOperationException("Бронювання не знайдено.");
    }

    public async Task<BookingDto> CompleteAsync(string guestId, Guid bookingId, CancellationToken cancellationToken = default)
    {
        var booking = await _db.Bookings
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.GuestId == guestId, cancellationToken)
            ?? throw new InvalidOperationException("Бронювання не знайдено.");

        if (booking.Status != BookingStatus.Confirmed)
        {
            throw new InvalidOperationException("Завершити можна лише підтверджене бронювання.");
        }

        if (booking.CheckOut > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new InvalidOperationException("Бронювання ще не закінчилось.");
        }

        booking.Status = BookingStatus.Completed;
        await _db.SaveChangesAsync(cancellationToken);
        return await MapAsync(booking.Id, cancellationToken) ?? throw new InvalidOperationException("Бронювання не знайдено.");
    }

    private async Task<BookingDto?> MapAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _db.Bookings
            .AsNoTracking()
            .Where(b => b.Id == id)
            .Select(b => new BookingDto(
                b.Id,
                b.ListingId,
                b.Listing!.Title,
                b.Listing.City,
                b.CheckIn,
                b.CheckOut,
                b.GuestsCount,
                b.PaymentPlan,
                b.TotalAmount,
                b.Status,
                b.MessageToHost))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
