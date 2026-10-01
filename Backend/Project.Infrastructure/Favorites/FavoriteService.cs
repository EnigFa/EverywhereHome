using Microsoft.EntityFrameworkCore;
using Project.Application.Entities;
using Project.Application.Favorites;
using Project.Application.Listings;
using Project.Infrastructure.Data;

namespace Project.Infrastructure.Favorites;

public class FavoriteService : IFavoriteService
{
    private readonly AppDbContext _db;

    public FavoriteService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ListingCardDto>> ListMineAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _db.Favorites
            .AsNoTracking()
            .Where(f => f.UserId == userId && f.Listing!.IsPublished && !f.Listing.Host!.IsBlocked)
            .OrderBy(f => f.Listing!.Title)
            .Select(f => new ListingCardDto(
                f.Listing!.Id,
                f.Listing.Title,
                f.Listing.City,
                f.Listing.Region,
                f.Listing.Country,
                f.Listing.PricePerNight,
                f.Listing.Reviews.Count == 0 ? 0 : f.Listing.Reviews.Average(r => r.Rating),
                f.Listing.Reviews.Count,
                f.Listing.Photos.OrderBy(p => p.SortOrder).Select(p => p.Url).FirstOrDefault(),
                f.Listing.Category,
                f.Listing.Latitude,
                f.Listing.Longitude))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(string userId, Guid listingId, CancellationToken cancellationToken = default)
    {
        var listing = await _db.Listings
            .FirstOrDefaultAsync(l => l.Id == listingId && l.IsPublished, cancellationToken)
            ?? throw new InvalidOperationException("Оголошення не знайдено.");

        if (listing.HostId == userId)
        {
            throw new InvalidOperationException("Не можна додати власне житло в обране.");
        }

        var exists = await _db.Favorites.AnyAsync(f => f.UserId == userId && f.ListingId == listingId, cancellationToken);
        if (exists)
        {
            return;
        }

        _db.Favorites.Add(new Favorite { UserId = userId, ListingId = listingId });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(string userId, Guid listingId, CancellationToken cancellationToken = default)
    {
        var favorite = await _db.Favorites
            .FirstOrDefaultAsync(f => f.UserId == userId && f.ListingId == listingId, cancellationToken);
        if (favorite is null)
        {
            return;
        }

        _db.Favorites.Remove(favorite);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
