namespace Project.Application.Entities;

public class Review
{
    public Guid Id { get; set; }
    public Guid ListingId { get; set; }
    public Listing? Listing { get; set; }
    public string AuthorId { get; set; } = string.Empty;
    public AppUser? Author { get; set; }
    public Guid? BookingId { get; set; }
    public decimal Rating { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
