using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Project.Application.Admin;
using Project.Application.Auth;
using Project.Application.Entities;
using Project.Application.Bookings;
using Project.Application.Favorites;
using Project.Application.Reviews;
using Project.Application.HostApplications;
using Project.Application.HostListings;
using Project.Application.Listings;
using Project.Application.Profiles;
using Project.Infrastructure.Admin;
using Project.Infrastructure.Auth;
using Project.Infrastructure.Bookings;
using Project.Infrastructure.Favorites;
using Project.Infrastructure.Reviews;
using Project.Infrastructure.Data;
using Project.Application.Files;
using Project.Infrastructure.Files;
using Project.Infrastructure.HostApplications;
using Project.Infrastructure.HostListings;
using Project.Infrastructure.Listings;
using Project.Infrastructure.Profiles;

namespace Project.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=(localdb)\\mssqllocaldb;Database=EverywhereHome;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        services.AddDbContext<AppDbContext>(options =>
        {
            if (IsPostgreSql(connectionString))
            {
                options.UseNpgsql(connectionString);
            }
            else
            {
                options.UseSqlServer(connectionString);
            }
        });

        services.AddIdentityCore<AppUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireDigit = false;
            })
            .AddRoles<IdentityRole>()
            .AddSignInManager()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddHttpContextAccessor();

        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IListingService, ListingService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IFavoriteService, FavoriteService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<IHostListingService, HostListingService>();
        services.AddScoped<IHostApplicationService, HostApplicationService>();
        services.AddScoped<IAdminUserService, AdminUserService>();
        services.AddSingleton<IFileStorage, LocalFileStorage>();

        return services;
    }

    public static bool IsPostgreSql(string connectionString) =>
        connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase)
        && !connectionString.Contains("localdb", StringComparison.OrdinalIgnoreCase);
}
