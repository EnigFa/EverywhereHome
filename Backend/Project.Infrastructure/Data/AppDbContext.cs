using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Project.Application.Entities;

namespace Project.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<AppUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Listing> Listings => Set<Listing>();
    public DbSet<ListingPhoto> ListingPhotos => Set<ListingPhoto>();
    public DbSet<ListingCategoryLink> ListingCategoryLinks => Set<ListingCategoryLink>();
    public DbSet<Amenity> Amenities => Set<Amenity>();
    public DbSet<ListingAmenity> ListingAmenities => Set<ListingAmenity>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Listing>(entity =>
        {
            entity.Property(x => x.PricePerNight).HasPrecision(10, 2);
            entity.Property(x => x.CleaningFee).HasPrecision(10, 2);
            entity.HasOne(x => x.Host)
                .WithMany()
                .HasForeignKey(x => x.HostId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ListingPhoto>()
            .HasOne(x => x.Listing)
            .WithMany(x => x.Photos)
            .HasForeignKey(x => x.ListingId);

        builder.Entity<ListingCategoryLink>(entity =>
        {
            entity.HasKey(x => new { x.ListingId, x.Category });
            entity.HasOne(x => x.Listing)
                .WithMany(x => x.CategoryLinks)
                .HasForeignKey(x => x.ListingId);
        });

        builder.Entity<ListingAmenity>()
            .HasKey(x => new { x.ListingId, x.AmenityId });

        builder.Entity<Favorite>()
            .HasKey(x => new { x.UserId, x.ListingId });

        builder.Entity<Review>(entity =>
        {
            entity.Property(x => x.Rating).HasPrecision(3, 2);
            entity.HasOne(x => x.Listing)
                .WithMany(x => x.Reviews)
                .HasForeignKey(x => x.ListingId);
            entity.HasOne(x => x.Author)
                .WithMany()
                .HasForeignKey(x => x.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Booking>(entity =>
        {
            entity.Property(x => x.TotalAmount).HasPrecision(10, 2);
            entity.HasOne(x => x.Listing)
                .WithMany(x => x.Bookings)
                .HasForeignKey(x => x.ListingId);
            entity.HasOne(x => x.Guest)
                .WithMany()
                .HasForeignKey(x => x.GuestId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Payment>(entity =>
        {
            entity.Property(x => x.Amount).HasPrecision(10, 2);
            entity.HasOne(x => x.Booking)
                .WithMany(x => x.Payments)
                .HasForeignKey(x => x.BookingId);
        });
    }
}
