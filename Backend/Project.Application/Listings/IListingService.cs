using Project.Application.Entities;

namespace Project.Application.Listings;

public record ListingCardDto(
    Guid Id,
    string Title,
    string City,
    string Region,
    string Country,
    decimal PricePerNight,
    decimal Rating,
    int ReviewCount,
    string? CoverPhotoUrl,
    ListingCategory Category,
    double Latitude,
    double Longitude);

public record ListingSearchQuery(
    string? Title,
    string? City,
    DateOnly? CheckIn,
    DateOnly? CheckOut,
    int? Guests,
    decimal? MinPrice,
    decimal? MaxPrice,
    IReadOnlyList<ListingCategory>? Categories,
    string? HostId,
    int Page,
    int PageSize);

public record ListingSearchResultDto(
    IReadOnlyList<ListingCardDto> Items,
    int Total,
    int Page,
    int PageSize);

public record HostSummaryDto(string DisplayName, string? AvatarUrl, string Id, int TrustLevel);

public record ReviewItemDto(Guid Id, string AuthorName, decimal Rating, string Text, DateTime CreatedAtUtc);

public record OccupiedStayDto(DateOnly CheckIn, DateOnly CheckOut);

public record ListingDetailDto(
    Guid Id,
    string Title,
    string Description,
    string City,
    string Region,
    string Country,
    string Address,
    decimal PricePerNight,
    decimal CleaningFee,
    decimal Rating,
    int ReviewCount,
    ListingCategory Category,
    double Latitude,
    double Longitude,
    int MaxGuests,
    int Bedrooms,
    int Beds,
    int Bathrooms,
    string HouseRules,
    string SafetyRules,
    string CancellationPolicy,
    IReadOnlyList<string> PhotoUrls,
    IReadOnlyList<string> Amenities,
    HostSummaryDto Host,
    IReadOnlyList<ReviewItemDto> Reviews,
    IReadOnlyList<OccupiedStayDto> OccupiedStays,
    bool IsPublished);

public interface IListingService
{
    Task<ListingSearchResultDto> SearchAsync(ListingSearchQuery query, CancellationToken cancellationToken = default);
    Task<ListingDetailDto?> GetByIdAsync(Guid id, string? viewerId = null, CancellationToken cancellationToken = default);
}
