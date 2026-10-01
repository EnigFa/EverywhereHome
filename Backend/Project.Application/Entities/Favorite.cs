namespace Project.Application.Entities;

public class Favorite
{
    public string UserId { get; set; } = string.Empty;
    public AppUser? User { get; set; }
    public Guid ListingId { get; set; }
    public Listing? Listing { get; set; }
}
