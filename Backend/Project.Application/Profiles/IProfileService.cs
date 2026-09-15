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
    string? Intro);

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

public interface IProfileService
{
    Task<ProfileDto?> GetAsync(string userId, CancellationToken cancellationToken = default);
    Task<ProfileDto> UpdateAsync(string userId, UpdateProfileRequest request, CancellationToken cancellationToken = default);
}
