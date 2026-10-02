using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Project.Application.Entities;
using Project.Application.Profiles;
using Project.Infrastructure.Data;

namespace Project.Infrastructure.Profiles;

public class ProfileService : IProfileService
{
    private readonly UserManager<AppUser> _users;
    private readonly AppDbContext _db;

    public ProfileService(UserManager<AppUser> users, AppDbContext db)
    {
        _users = users;
        _db = db;
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

    public async Task<PublicProfileDto?> GetPublicAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _users.FindByIdAsync(userId);
        if (user is null)
        {
            return null;
        }

        var rows = await _db.Listings.AsNoTracking()
            .Where(l => l.HostId == user.Id && l.IsPublished)
            .Select(l => new
            {
                l.Id,
                l.Title,
                l.City,
                l.PricePerNight,
                l.CreatedAtUtc,
                ReviewCount = l.Reviews.Count,
                Rating = l.Reviews.Count == 0 ? 0m : l.Reviews.Average(r => r.Rating),
                Cover = l.Photos.OrderBy(p => p.SortOrder).Select(p => p.Url).FirstOrDefault()
            })
            .ToListAsync(cancellationToken);
        var listings = rows
            .OrderByDescending(l => l.ReviewCount > 0)
            .ThenByDescending(l => l.Rating)
            .ThenByDescending(l => l.CreatedAtUtc)
            .Take(3)
            .Select(l => new PublicListingDto(l.Id, l.Title, l.City, l.PricePerNight, l.Rating, l.ReviewCount, l.Cover))
            .ToList();
        var reviewCount = rows.Sum(l => l.ReviewCount);
        return new PublicProfileDto(
            user.Id,
            user.DisplayName,
            user.AvatarUrl,
            user.IsHost,
            user.IsAdmin || user.IsChiefAdmin,
            user.Hometown,
            user.Profession,
            user.Languages,
            user.Intro,
            rows.Count,
            reviewCount,
            listings,
            user.IsBlocked,
            user.TrustLevel,
            user.IsChiefAdmin);
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
        user.Intro,
        user.EmailConfirmed,
        user.Id,
        user.IsAdmin || user.IsChiefAdmin,
        user.IsChiefAdmin);
}
