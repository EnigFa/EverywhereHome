using Microsoft.AspNetCore.Identity;
using Project.Application.Entities;
using Project.Application.Profiles;

namespace Project.Infrastructure.Profiles;

public class ProfileService : IProfileService
{
    private readonly UserManager<AppUser> _users;

    public ProfileService(UserManager<AppUser> users)
    {
        _users = users;
    }

    public async Task<ProfileDto?> GetAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _users.FindByIdAsync(userId);
        return user is null ? null : Map(user);
    }

    public async Task<ProfileDto> UpdateAsync(string userId, UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _users.FindByIdAsync(userId)
            ?? throw new InvalidOperationException("Користувача не знайдено.");

        user.DisplayName = request.DisplayName.Trim();
        user.School = request.School;
        user.Profession = request.Profession;
        user.Languages = request.Languages;
        user.Hometown = request.Hometown;
        user.BirthDecade = request.BirthDecade;
        user.Passion = request.Passion;
        user.UselessSkills = request.UselessSkills;
        user.TimeSink = request.TimeSink;
        user.FavoriteSong = request.FavoriteSong;
        user.FunFact = request.FunFact;
        user.BiographyTitle = request.BiographyTitle;
        user.Pets = request.Pets;
        user.Intro = request.Intro;

        var result = await _users.UpdateAsync(user);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        return Map(user);
    }

    private static ProfileDto Map(AppUser user) => new(
        user.Email ?? string.Empty,
        user.DisplayName,
        user.AvatarUrl,
        user.IsHost,
        user.PhoneVerified,
        user.PhoneNumber,
        user.School,
        user.Profession,
        user.Languages,
        user.Hometown,
        user.BirthDecade,
        user.Passion,
        user.UselessSkills,
        user.TimeSink,
        user.FavoriteSong,
        user.FunFact,
        user.BiographyTitle,
        user.Pets,
        user.Intro);
}
