namespace Project.Application.Entities;

public class ListingCategoryLink
{
    public Guid ListingId { get; set; }
    public Listing? Listing { get; set; }
    public ListingCategory Category { get; set; }
}
