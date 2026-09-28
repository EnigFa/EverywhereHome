using Microsoft.EntityFrameworkCore;
using Project.Application.Entities;
using Project.Application.Listings;
using Project.Application.Reviews;
using Project.Infrastructure.Data;

namespace Project.Infrastructure.Reviews;

public class ReviewService : IReviewService
{
    private readonly AppDbContext _db;

    public ReviewService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ReviewItemDto> CreateAsync(string userId, Guid listingId, CreateReviewRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Rating is < 1 or > 5)
        {
            throw new InvalidOperationException("Оцінка має бути від 1 до 5.");
        }

        if (string.IsNullOrWhiteSpace(request.Text))
        {
            throw new InvalidOperationException("Напишіть текст відгуку.");
        }

        var listing = await _db.Listings
            .FirstOrDefaultAsync(l => l.Id == listingId && l.IsPublished, cancellationToken)
            ?? throw new InvalidOperationException("Оголошення не знайдено.");

        if (listing.HostId == userId)
        {
            throw new InvalidOperationException("Не можна залишити відгук на власне житло.");
        }

        var booking = await _db.Bookings
            .Where(b => b.ListingId == listingId && b.GuestId == userId && b.Status == BookingStatus.Completed)
            .OrderByDescending(b => b.CheckOut)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("Відгук можна залишити після завершеного проживання.");

        var already = await _db.Reviews.AnyAsync(r => r.ListingId == listingId && r.AuthorId == userId, cancellationToken);
        if (already)
        {
            throw new InvalidOperationException("Ви вже залишили відгук на це житло.");
        }

        var author = await _db.Users.FirstAsync(u => u.Id == userId, cancellationToken);
        var review = new Review
        {
            Id = Guid.NewGuid(),
            ListingId = listingId,
            AuthorId = userId,
            BookingId = booking.Id,
            Rating = request.Rating,
            Text = request.Text.Trim()
        };
        _db.Reviews.Add(review);
        await _db.SaveChangesAsync(cancellationToken);
        return new ReviewItemDto(review.Id, author.DisplayName, review.Rating, review.Text, review.CreatedAtUtc);
    }
}
