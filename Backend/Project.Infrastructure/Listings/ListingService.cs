using Microsoft.EntityFrameworkCore;
using Project.Application.Entities;
using Project.Application.Listings;
using Project.Infrastructure.Data;

namespace Project.Infrastructure.Listings;

public class ListingService : IListingService
{
    private readonly AppDbContext _db;

    public ListingService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ListingSearchResultDto> SearchAsync(ListingSearchQuery query, CancellationToken cancellationToken = default)
    {
        var listings = _db.Listings
            .AsNoTracking()
            .Where(l => l.IsPublished && !l.Host!.IsBlocked);

        if (!string.IsNullOrWhiteSpace(query.Title))
        {
            var term = query.Title.Trim();
            listings = listings.Where(l => l.Title.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(query.City))
        {
            var city = query.City.Trim();
            listings = listings.Where(l => l.City.Contains(city));
        }

        if (query.Guests is > 0)
        {
            listings = listings.Where(l => l.MaxGuests >= query.Guests);
        }

        if (query.MinPrice is > 0)
        {
            listings = listings.Where(l => l.PricePerNight >= query.MinPrice);
        }

        if (query.MaxPrice is > 0)
        {
            listings = listings.Where(l => l.PricePerNight <= query.MaxPrice);
        }

        if (query.Categories is { Count: > 0 })
        {
            var cats = query.Categories.ToList();
            listings = listings.Where(l =>
                cats.Contains(l.Category) ||
                l.CategoryLinks.Any(link => cats.Contains(link.Category)));
        }

        if (!string.IsNullOrWhiteSpace(query.HostId))
        {
            var hostId = query.HostId.Trim();
            listings = listings.Where(l => l.HostId == hostId);
        }

        if (query.CheckIn is not null && query.CheckOut is not null && query.CheckOut > query.CheckIn)
        {
            var checkIn = query.CheckIn.Value;
            var checkOut = query.CheckOut.Value;
            listings = listings.Where(l => !l.Bookings.Any(b =>
                b.Status == BookingStatus.Confirmed &&
                b.CheckIn < checkOut &&
                b.CheckOut > checkIn));
        }

        var pageSize = query.PageSize is 50 or 100 ? query.PageSize : 25;
        var page = query.Page < 1 ? 1 : query.Page;
        var total = await listings.CountAsync(cancellationToken);
        var items = await listings
            .OrderBy(l => l.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new ListingCardDto(
                l.Id,
                l.Title,
                l.City,
                l.Region,
                l.Country,
                l.PricePerNight,
                l.Reviews.Count == 0 ? 0 : l.Reviews.Average(r => r.Rating),
                l.Reviews.Count,
                l.Photos.OrderBy(p => p.SortOrder).Select(p => p.Url).FirstOrDefault(),
                l.Category,
                l.Latitude,
                l.Longitude))
            .ToListAsync(cancellationToken);

        return new ListingSearchResultDto(items, total, page, pageSize);
    }

    public async Task<ListingDetailDto?> GetByIdAsync(Guid id, string? viewerId = null, CancellationToken cancellationToken = default)
    {
        return await _db.Listings
            .AsNoTracking()
            .Where(l => l.Id == id && (
                (l.IsPublished && !l.Host!.IsBlocked)
                || (viewerId != null && (l.HostId == viewerId || _db.Users.Any(u => u.Id == viewerId && !u.IsBlocked && (u.IsAdmin || u.IsChiefAdmin))))))
            .Select(l => new ListingDetailDto(
                l.Id,
                l.Title,
                l.Description,
                l.City,
                l.Region,
                l.Country,
                l.Address,
                l.PricePerNight,
                l.CleaningFee,
                l.Reviews.Count == 0 ? 0 : l.Reviews.Average(r => r.Rating),
                l.Reviews.Count,
                l.Category,
                l.Latitude,
                l.Longitude,
                l.MaxGuests,
                l.Bedrooms,
                l.Beds,
                l.Bathrooms,
                l.HouseRules,
                l.SafetyRules,
                l.CancellationPolicy,
                l.Photos.OrderBy(p => p.SortOrder).Select(p => p.Url).ToList(),
                l.Amenities.Select(a => a.Amenity!.Name).ToList(),
                new HostSummaryDto(l.Host!.DisplayName, l.Host.AvatarUrl, l.Host.Id, l.Host.TrustLevel),
                l.Reviews
                    .OrderByDescending(r => r.CreatedAtUtc)
                    .Select(r => new ReviewItemDto(r.Id, r.Author!.DisplayName, r.Rating, r.Text, r.CreatedAtUtc))
                    .ToList(),
                l.Bookings
                    .Where(b => b.Status == BookingStatus.Confirmed)
                    .OrderBy(b => b.CheckIn)
                    .Select(b => new OccupiedStayDto(b.CheckIn, b.CheckOut))
                    .ToList(),
                l.IsPublished))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
