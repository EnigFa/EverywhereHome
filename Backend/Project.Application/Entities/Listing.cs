namespace Project.Application.Entities;

public class Listing
{
    public Guid Id { get; set; }
    public string HostId { get; set; } = string.Empty;
    public AppUser? Host { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ListingCategory Category { get; set; }
    public string City { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string Country { get; set; } = "Україна";
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public decimal PricePerNight { get; set; }
    public decimal CleaningFee { get; set; }
    public int MaxGuests { get; set; }
    public int Bedrooms { get; set; }
    public int Beds { get; set; }
    public int Bathrooms { get; set; }

    public string HouseRules { get; set; } = string.Empty;
    public string SafetyRules { get; set; } = string.Empty;
    public string CancellationPolicy { get; set; } = string.Empty;

    public bool IsPublished { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<ListingPhoto> Photos { get; set; } = new List<ListingPhoto>();
    public ICollection<ListingAmenity> Amenities { get; set; } = new List<ListingAmenity>();
    public ICollection<ListingCategoryLink> CategoryLinks { get; set; } = new List<ListingCategoryLink>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
}
