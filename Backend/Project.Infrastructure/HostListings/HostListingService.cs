using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Project.Application.Entities;
using Project.Application.Files;
using Project.Application.HostListings;
using Project.Infrastructure.Data;

namespace Project.Infrastructure.HostListings;

public class HostListingService : IHostListingService
{
    private const int MaxPhotos = 12;
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _users;
    private readonly IFileStorage _files;

    public HostListingService(AppDbContext db, UserManager<AppUser> users, IFileStorage files)
    {
        _db = db;
        _users = users;
        _files = files;
    }

    public async Task<IReadOnlyList<HostListingDto>> ListMineAsync(string hostId, CancellationToken cancellationToken = default)
    {
        return await _db.Listings
            .AsNoTracking()
            .Where(l => l.HostId == hostId)
            .OrderByDescending(l => l.CreatedAtUtc)
            .Select(l => new HostListingDto(l.Id, l.Title, l.City, l.PricePerNight, l.IsPublished, l.Category))
            .ToListAsync(cancellationToken);
    }

    public async Task<HostListingEditDto> GetMineAsync(string hostId, Guid id, CancellationToken cancellationToken = default)
    {
        var listing = await FindMineAsync(hostId, id, cancellationToken);
        return ToEditDto(listing);
    }

    public async Task<HostListingDto> CreateAsync(string hostId, HostListingInput input, CancellationToken cancellationToken = default)
    {
        Validate(input);
        var user = await _users.FindByIdAsync(hostId) ?? throw new InvalidOperationException("Користувача не знайдено.");
        if (!user.IsHost)
        {
            throw new InvalidOperationException("Створювати оголошення може лише підтверджений господар.");
        }

        var listing = Apply(new Listing { Id = Guid.NewGuid(), HostId = hostId }, input);
        await ReplaceAmenitiesAsync(listing, input.Amenities, cancellationToken);
        _db.Listings.Add(listing);
        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(listing);
    }

    public async Task<HostListingDto> UpdateAsync(string hostId, Guid id, HostListingInput input, CancellationToken cancellationToken = default)
    {
        Validate(input);
        var listing = await FindMineAsync(hostId, id, cancellationToken);
        Apply(listing, input);
        await ReplaceAmenitiesAsync(listing, input.Amenities, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(listing);
    }

    public async Task DeleteAsync(string hostId, Guid id, CancellationToken cancellationToken = default)
    {
        var listing = await FindMineAsync(hostId, id, cancellationToken);
        var hasConfirmed = await _db.Bookings.AnyAsync(
            b => b.ListingId == id && b.Status == BookingStatus.Confirmed,
            cancellationToken);
        if (hasConfirmed)
        {
            listing.IsPublished = false;
            await _db.SaveChangesAsync(cancellationToken);
            return;
        }

        _db.Listings.Remove(listing);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<HostPhotoDto> AddPhotoAsync(string hostId, Guid listingId, Stream content, string fileName, CancellationToken cancellationToken = default)
    {
        var listing = await FindMineAsync(hostId, listingId, cancellationToken);
        if (listing.Photos.Count >= MaxPhotos)
        {
            throw new InvalidOperationException("Можна додати щонайбільше 12 фото.");
        }

        var url = await _files.SaveListingPhotoAsync(content, fileName, cancellationToken);
        var photo = new ListingPhoto
        {
            Id = Guid.NewGuid(),
            ListingId = listing.Id,
            Url = url,
            SortOrder = listing.Photos.Count == 0 ? 0 : listing.Photos.Max(p => p.SortOrder) + 1
        };
        listing.Photos.Add(photo);
        await _db.SaveChangesAsync(cancellationToken);
        return new HostPhotoDto(photo.Id, photo.Url);
    }

    public async Task DeletePhotoAsync(string hostId, Guid listingId, Guid photoId, CancellationToken cancellationToken = default)
    {
        var listing = await FindMineAsync(hostId, listingId, cancellationToken);
        var photo = listing.Photos.FirstOrDefault(p => p.Id == photoId)
            ?? throw new InvalidOperationException("Фото не знайдено.");
        _files.TryDeleteLocal(photo.Url);
        listing.Photos.Remove(photo);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Listing> FindMineAsync(string hostId, Guid id, CancellationToken cancellationToken)
    {
        return await _db.Listings
            .Include(l => l.Photos)
            .Include(l => l.CategoryLinks)
            .Include(l => l.Amenities)
            .ThenInclude(a => a.Amenity)
            .FirstOrDefaultAsync(l => l.Id == id && l.HostId == hostId, cancellationToken)
            ?? throw new InvalidOperationException("Оголошення не знайдено.");
    }

    private static void Validate(HostListingInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Title) || string.IsNullOrWhiteSpace(input.City))
        {
            throw new InvalidOperationException("Назва і місто обовʼязкові.");
        }

        if (input.PricePerNight <= 0 || input.MaxGuests < 1)
        {
            throw new InvalidOperationException("Перевірте ціну та кількість гостей.");
        }

        if (NormalizeCategories(input).Count == 0)
        {
            throw new InvalidOperationException("Оберіть хоча б одну категорію.");
        }
    }

    private static List<ListingCategory> NormalizeCategories(HostListingInput input)
    {
        var cats = (input.Categories ?? [])
            .Distinct()
            .ToList();
        return cats;
    }

    private static Listing Apply(Listing listing, HostListingInput input)
    {
        var categories = NormalizeCategories(input);
        listing.Title = input.Title.Trim();
        listing.Description = input.Description.Trim();
        listing.Category = categories[0];
        listing.City = input.City.Trim();
        listing.Region = input.Region.Trim();
        listing.Address = input.Address.Trim();
        listing.PricePerNight = input.PricePerNight;
        listing.CleaningFee = input.CleaningFee;
        listing.MaxGuests = input.MaxGuests;
        listing.Bedrooms = input.Bedrooms;
        listing.Beds = input.Beds;
        listing.Bathrooms = Math.Max(1, input.Bathrooms);
        listing.HouseRules = input.HouseRules.Trim();
        listing.IsPublished = input.IsPublished;
        listing.CategoryLinks.Clear();
        foreach (var category in categories)
        {
            listing.CategoryLinks.Add(new ListingCategoryLink { ListingId = listing.Id, Category = category });
        }

        return listing;
    }

    private async Task ReplaceAmenitiesAsync(Listing listing, IReadOnlyList<string>? names, CancellationToken cancellationToken)
    {
        if (names is null)
        {
            return;
        }

        var wanted = names
            .Select(name => name.Trim())
            .Where(name => name.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        listing.Amenities.Clear();
        foreach (var name in wanted)
        {
            var amenity = await _db.Amenities.FirstOrDefaultAsync(
                item => item.Name.ToLower() == name.ToLower(),
                cancellationToken);
            if (amenity is null)
            {
                amenity = new Amenity { Id = Guid.NewGuid(), Name = name };
                _db.Amenities.Add(amenity);
            }

            listing.Amenities.Add(new ListingAmenity { ListingId = listing.Id, AmenityId = amenity.Id });
        }
    }

    private static HostListingEditDto ToEditDto(Listing listing)
    {
        var categories = listing.CategoryLinks.Select(l => l.Category).Distinct().ToList();
        if (categories.Count == 0)
        {
            categories.Add(listing.Category);
        }

        return new(
            listing.Id,
            listing.Title,
            listing.Description,
            listing.Category,
            categories,
            listing.City,
            listing.Region,
            listing.Address,
            listing.PricePerNight,
            listing.CleaningFee,
            listing.MaxGuests,
            listing.Bedrooms,
            listing.Beds,
            listing.Bathrooms,
            listing.HouseRules,
            listing.IsPublished,
            listing.Photos.OrderBy(p => p.SortOrder).Select(p => new HostPhotoDto(p.Id, p.Url)).ToList(),
            listing.Amenities.Select(a => a.Amenity!.Name).Where(name => !string.IsNullOrWhiteSpace(name)).ToList());
    }

    private static HostListingDto ToDto(Listing listing) =>
        new(listing.Id, listing.Title, listing.City, listing.PricePerNight, listing.IsPublished, listing.Category);
}
