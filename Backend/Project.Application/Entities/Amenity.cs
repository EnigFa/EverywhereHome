namespace Project.Application.Entities;

public class Amenity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<ListingAmenity> Listings { get; set; } = new List<ListingAmenity>();
}

public class ListingAmenity
{
    public Guid ListingId { get; set; }
    public Listing? Listing { get; set; }
    public Guid AmenityId { get; set; }
    public Amenity? Amenity { get; set; }
}
