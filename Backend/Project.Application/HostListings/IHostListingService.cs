using Project.Application.Entities;

namespace Project.Application.HostListings;

public record HostListingInput(
    string Title,
    string Description,
    ListingCategory Category,
    IReadOnlyList<ListingCategory>? Categories,
    string City,
    string Region,
    string Address,
    decimal PricePerNight,
    decimal CleaningFee,
    int MaxGuests,
    int Bedrooms,
    int Beds,
    int Bathrooms,
    string HouseRules,
    bool IsPublished,
    IReadOnlyList<string>? Amenities);

public record HostListingDto(
    Guid Id,
    string Title,
    string City,
    decimal PricePerNight,
    bool IsPublished,
    ListingCategory Category);

public record HostListingEditDto(
    Guid Id,
    string Title,
    string Description,
    ListingCategory Category,
    IReadOnlyList<ListingCategory> Categories,
    string City,
    string Region,
    string Address,
    decimal PricePerNight,
    decimal CleaningFee,
    int MaxGuests,
    int Bedrooms,
    int Beds,
    int Bathrooms,
    string HouseRules,
    bool IsPublished,
    IReadOnlyList<HostPhotoDto> Photos,
    IReadOnlyList<string> Amenities);

public record HostPhotoDto(Guid Id, string Url);

public interface IHostListingService
{
    Task<IReadOnlyList<HostListingDto>> ListMineAsync(string hostId, CancellationToken cancellationToken = default);
    Task<HostListingEditDto> GetMineAsync(string hostId, Guid id, CancellationToken cancellationToken = default);
    Task<HostListingDto> CreateAsync(string hostId, HostListingInput input, CancellationToken cancellationToken = default);
    Task<HostListingDto> UpdateAsync(string hostId, Guid id, HostListingInput input, CancellationToken cancellationToken = default);
    Task<HostListingEditDto> GetAnyAsync(string adminId, Guid id, CancellationToken cancellationToken = default);
    Task<HostListingDto> UpdateAnyAsync(string adminId, Guid id, HostListingInput input, CancellationToken cancellationToken = default);
    Task UnpublishAnyAsync(string adminId, Guid id, CancellationToken cancellationToken = default);
    Task<HostPhotoDto> AddPhotoAnyAsync(string adminId, Guid listingId, Stream content, string fileName, CancellationToken cancellationToken = default);
    Task DeletePhotoAnyAsync(string adminId, Guid listingId, Guid photoId, CancellationToken cancellationToken = default);
    Task DeleteAsync(string hostId, Guid id, CancellationToken cancellationToken = default);
    Task<HostPhotoDto> AddPhotoAsync(string hostId, Guid listingId, Stream content, string fileName, CancellationToken cancellationToken = default);
    Task DeletePhotoAsync(string hostId, Guid listingId, Guid photoId, CancellationToken cancellationToken = default);
}
