using Microsoft.AspNetCore.Identity;

namespace Project.Application.Entities;

public class AppUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public bool IsHost { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsChiefAdmin { get; set; }
    public bool IsBlocked { get; set; }
    public int TrustLevel { get; set; }
    public bool PhoneVerified { get; set; }

    public string? School { get; set; }
    public string? Profession { get; set; }
    public string? Languages { get; set; }
    public string? Hometown { get; set; }
    public string? BirthDecade { get; set; }
    public string? Passion { get; set; }
    public string? UselessSkills { get; set; }
    public string? TimeSink { get; set; }
    public string? FavoriteSong { get; set; }
    public string? FunFact { get; set; }
    public string? BiographyTitle { get; set; }
    public string? Pets { get; set; }
    public string? Intro { get; set; }
}
