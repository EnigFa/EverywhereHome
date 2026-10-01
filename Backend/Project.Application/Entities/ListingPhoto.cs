namespace Project.Application.Entities;

public class ListingPhoto
{
    public Guid Id { get; set; }
    public Guid ListingId { get; set; }
    public Listing? Listing { get; set; }
    public string Url { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
