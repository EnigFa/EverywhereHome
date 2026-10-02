namespace Project.Application.Profiles;

public record ProfileDto(
    string Email,
    string DisplayName,
    string? AvatarUrl,
    bool IsHost,
    bool PhoneVerified,
    string? PhoneNumber,
    string? School,
    string? Profession,
    string? Languages,
    string? Hometown,
    string? BirthDecade,
    string? Passion,
    string? UselessSkills,
    string? TimeSink,
    string? FavoriteSong,
    string? FunFact,
    string? BiographyTitle,
    string? Pets,
    string? Intro,
    bool EmailConfirmed,
    string Id,
    bool IsAdmin,
    bool IsChiefAdmin);

public record UpdateProfileRequest(
    string DisplayName,
    string? School,
    string? Profession,
    string? Languages,
    string? Hometown,
    string? BirthDecade,
    string? Passion,
    string? UselessSkills,
    string? TimeSink,
    string? FavoriteSong,
    string? FunFact,
    string? BiographyTitle,
    string? Pets,
    string? Intro);

public record PublicListingDto(Guid Id, string Title, string City, decimal PricePerNight, decimal Rating, int ReviewCount, string? CoverPhotoUrl);

public record PublicProfileDto(
    string Id,
    string DisplayName,
    string? AvatarUrl,
    bool IsHost,
    bool IsAdmin,
    string? Hometown,
    string? Profession,
    string? Languages,
    string? Intro,
    int ListingCount,
    int ReviewCount,
    IReadOnlyList<PublicListingDto> Listings,
    bool IsBlocked,
    int TrustLevel,
    bool IsChiefAdmin);

public interface IProfileService
{
    Task<ProfileDto?> GetAsync(string userId, CancellationToken cancellationToken = default);
    Task<ProfileDto> UpdateAsync(string userId, UpdateProfileRequest request, CancellationToken cancellationToken = default);
    Task<PublicProfileDto?> GetPublicAsync(string userId, CancellationToken cancellationToken = default);
}
