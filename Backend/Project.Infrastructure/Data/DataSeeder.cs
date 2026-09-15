using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Project.Application.Entities;

namespace Project.Infrastructure.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext db, UserManager<AppUser> users)
    {
        var host = await users.FindByEmailAsync("host@everywherehome.local");
        if (host is null)
        {
            host = new AppUser
            {
                UserName = "host@everywherehome.local",
                Email = "host@everywherehome.local",
                DisplayName = "Ілона",
                IsHost = true,
                EmailConfirmed = true
            };
            await users.CreateAsync(host, "Host123!");
        }

        if (await users.FindByEmailAsync("guest@everywherehome.local") is null)
        {
            await users.CreateAsync(new AppUser
            {
                UserName = "guest@everywherehome.local",
                Email = "guest@everywherehome.local",
                DisplayName = "Гість",
                EmailConfirmed = true
            }, "Guest123!");
        }

        var wifi = await EnsureAmenityAsync(db, "Wi-Fi");
        var kitchen = await EnsureAmenityAsync(db, "Кухня");
        var parking = await EnsureAmenityAsync(db, "Паркінг");
        var ac = await EnsureAmenityAsync(db, "Кондиціонер");
        await db.SaveChangesAsync();

        var existingTitles = await db.Listings.Select(l => l.Title).ToListAsync();
        var toAdd = Catalog().Where(item => !existingTitles.Contains(item.Title)).ToList();
        var amenityIds = new[] { wifi.Id, kitchen.Id, parking.Id, ac.Id };
        foreach (var item in toAdd)
        {
            var listing = new Listing
            {
                Id = Guid.NewGuid(),
                HostId = host.Id,
                Title = item.Title,
                Description = item.Description,
                Category = item.Category,
                City = item.City,
                Region = item.Region,
                Country = "Україна",
                Address = item.Address,
                Latitude = item.Latitude,
                Longitude = item.Longitude,
                PricePerNight = item.PricePerNight,
                CleaningFee = item.CleaningFee,
                MaxGuests = item.MaxGuests,
                Bedrooms = item.Bedrooms,
                Beds = item.Beds,
                Bathrooms = item.Bathrooms,
                HouseRules = item.HouseRules,
                SafetyRules = "Детектор диму та аптечка на місці.",
                CancellationPolicy = "Безкоштовне скасування за 5 днів до прибуття.",
                IsPublished = true
            };

            var order = 0;
            foreach (var url in PhotosFor(item.City, item.Category))
            {
                listing.Photos.Add(new ListingPhoto { Id = Guid.NewGuid(), Url = url, SortOrder = order++ });
            }

            listing.CategoryLinks.Add(new ListingCategoryLink { ListingId = listing.Id, Category = item.Category });

            foreach (var amenityId in amenityIds.Take(item.AmenityCount))
            {
                listing.Amenities.Add(new ListingAmenity { AmenityId = amenityId });
            }

            db.Listings.Add(listing);
        }

        if (toAdd.Count > 0)
        {
            await db.SaveChangesAsync();
        }

        await RefreshListingPhotosAsync(db);
        await EnsureCategoryLinksAsync(db);
    }

    private static async Task EnsureCategoryLinksAsync(AppDbContext db)
    {
        var listings = await db.Listings.Include(l => l.CategoryLinks).ToListAsync();
        var changed = false;
        foreach (var listing in listings)
        {
            if (listing.CategoryLinks.Count > 0)
            {
                continue;
            }

            db.ListingCategoryLinks.Add(new ListingCategoryLink { ListingId = listing.Id, Category = listing.Category });
            changed = true;
        }

        if (changed)
        {
            await db.SaveChangesAsync();
        }
    }

    private static async Task RefreshListingPhotosAsync(AppDbContext db)
    {
        var listings = await db.Listings.Include(l => l.Photos).ToListAsync();
        var changed = false;
        foreach (var listing in listings)
        {
            if (listing.Photos.Any(p => p.Url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var urls = PhotosFor(listing.City, listing.Category);
            var photos = listing.Photos.OrderBy(p => p.SortOrder).ToList();
            for (var i = 0; i < urls.Length; i++)
            {
                if (i < photos.Count)
                {
                    photos[i].Url = urls[i];
                    photos[i].SortOrder = i;
                }
                else
                {
                    db.ListingPhotos.Add(new ListingPhoto
                    {
                        Id = Guid.NewGuid(),
                        ListingId = listing.Id,
                        Url = urls[i],
                        SortOrder = i
                    });
                }
            }

            changed = true;
        }

        if (changed)
        {
            await db.SaveChangesAsync();
        }
    }

    private static string[] PhotosFor(string city, ListingCategory category)
    {
        string[] pool = category switch
        {
            ListingCategory.ScenicViews => [Pool[0], Pool[1], Pool[2], Pool[8]],
            ListingCategory.Luxe => [Pool[3], Pool[4], Pool[0], Pool[5]],
            ListingCategory.Hostels => [Pool[6], Pool[7], Pool[2], Pool[9]],
            ListingCategory.Countryside => [Pool[8], Pool[10], Pool[1], Pool[11]],
            ListingCategory.Designer => [Pool[5], Pool[4], Pool[2], Pool[3]],
            ListingCategory.CityCenter => [Pool[2], Pool[9], Pool[0], Pool[4]],
            ListingCategory.LargeApartments => [Pool[11], Pool[3], Pool[7], Pool[1]],
            _ => [Pool[9], Pool[6], Pool[2], Pool[5]]
        };
        var shift = Math.Abs(city.GetHashCode(StringComparison.Ordinal)) % pool.Length;
        return Enumerable.Range(0, 4).Select(i => pool[(shift + i) % pool.Length]).ToArray();
    }

    private static readonly string[] Pool =
    [
        "https://images.unsplash.com/photo-1502672260266-1c1ef2d93688?auto=format&fit=crop&w=1400&q=80",
        "https://images.unsplash.com/photo-1505691938895-1758d7feb511?auto=format&fit=crop&w=1400&q=80",
        "https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?auto=format&fit=crop&w=1400&q=80",
        "https://images.unsplash.com/photo-1600596542815-ffad4c1539a9?auto=format&fit=crop&w=1400&q=80",
        "https://images.unsplash.com/photo-1613490493576-7fde63acd811?auto=format&fit=crop&w=1400&q=80",
        "https://images.unsplash.com/photo-1493809842364-78817add7ffb?auto=format&fit=crop&w=1400&q=80",
        "https://images.unsplash.com/photo-1555854877-bab0e564b8d5?auto=format&fit=crop&w=1400&q=80",
        "https://images.unsplash.com/photo-1522771739844-6a9f6d5f14af?auto=format&fit=crop&w=1400&q=80",
        "https://images.unsplash.com/photo-1441974231531-c6227db76b6e?auto=format&fit=crop&w=1400&q=80",
        "https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?auto=format&fit=crop&w=1400&q=80",
        "https://images.unsplash.com/photo-1464822759023-fed622ff2c3b?auto=format&fit=crop&w=1400&q=80",
        "https://images.unsplash.com/photo-1560185893-a55cbc8c57bb?auto=format&fit=crop&w=1400&q=80"
    ];

    private static async Task<Amenity> EnsureAmenityAsync(AppDbContext db, string name)
    {
        var existing = await db.Amenities.FirstOrDefaultAsync(a => a.Name == name);
        if (existing is not null)
        {
            return existing;
        }

        var amenity = new Amenity { Id = Guid.NewGuid(), Name = name };
        db.Amenities.Add(amenity);
        return amenity;
    }

    private static IEnumerable<SeedItem> Catalog() =>
    [
        new("Студія та спальня з панорамою на місто! Біля моря!", "Квартира біля моря з терасою і окремим робочим місцем.", ListingCategory.ScenicViews, "Одеса", "Одеська область", "Аркадія", 46.438, 30.767, 63, 20, 4, 1, 2, 1, 2, "Тиша після 22:00."),
        new("Дизайнерська квартира в центрі міста", "Простора квартира біля Хрещатика, зручна для роботи і прогулянок.", ListingCategory.CityCenter, "Київ", "Київська область", "Центр", 50.4501, 30.5234, 89, 25, 3, 1, 1, 1, 3, "Не палити в приміщенні."),
        new("Luxe пентхаус з видом на Дніпро", "Панорамні вікна, джакузі та консьєрж. Для особливої подорожі.", ListingCategory.Luxe, "Київ", "Київська область", "Печерськ", 50.426, 30.538, 210, 40, 5, 2, 3, 2, 4, "Лише для гостей старше 21."),
        new("Затишна студія на Подолі", "Невелика квартира з усім необхідним біля метро.", ListingCategory.SmallApartments, "Київ", "Київська область", "Поділ", 50.469, 30.514, 42, 15, 2, 1, 1, 1, 2, "Без вечірок."),
        new("Велика квартира для компанії", "Три спальні, велика кухня-вітальня, зручно для друзів.", ListingCategory.LargeApartments, "Львів", "Львівська область", "Франківський", 49.833, 24.015, 95, 22, 8, 3, 4, 2, 3, "Поважайте сусідів."),
        new("Дизайнерський лофт у Львові", "Авторський інтер’єр у відреставрованій кам’яниці.", ListingCategory.Designer, "Львів", "Львівська область", "Площа Ринок", 49.841, 24.032, 78, 18, 3, 1, 1, 1, 3, "Взуття залишайте в передпокої."),
        new("Хостел у центрі Львова, ліжко в дормі", "Чиста кімната на 6 осіб, спільна кухня і lockers.", ListingCategory.Hostels, "Львів", "Львівська область", "Центр", 49.839, 24.029, 18, 5, 1, 1, 6, 1, 2, "Комендантська година кухні — 23:00."),
        new("Садиба серед садів", "Будинок з садом, мангалом і тишею за містом.", ListingCategory.Countryside, "Вінниця", "Вінницька область", "с. Якушинці", 49.270, 28.395, 70, 20, 6, 2, 3, 1, 3, "Не зривати квіти в саду."),
        new("Будинок у Карпатах з краєвидом", "Вид на гори, камін і тераса для сніданків.", ListingCategory.ScenicViews, "Яремче", "Івано-Франківська область", "Яремче", 48.458, 24.552, 85, 20, 5, 2, 3, 1, 4, "Дрова для каміна — у сараї."),
        new("Котедж у Карпатах, сільська тиша", "Дерев’яний будинок біля лісу, ідеально для вихідних.", ListingCategory.Countryside, "Буковель", "Івано-Франківська область", "Поляниця", 48.360, 24.415, 120, 30, 6, 3, 4, 2, 4, "Після катання — сушіть лижі надворі."),
        new("Luxe вілла біля моря", "Басейн, сад і 5 хвилин до пляжу.", ListingCategory.Luxe, "Одеса", "Одеська область", "Фонтан", 46.447, 30.755, 260, 50, 8, 4, 5, 3, 4, "Басейн лише для гостей вілли."),
        new("Квартира в центрі Харкова", "Піша доступність до парку Шевченка.", ListingCategory.CityCenter, "Харків", "Харківська область", "Сумська", 49.993, 36.230, 55, 16, 4, 1, 2, 1, 3, "Не залишайте вікна відчиненими."),
        new("Невелика студія біля вокзалу", "Зручна для однієї ночі або короткої поїздки.", ListingCategory.SmallApartments, "Дніпро", "Дніпропетровська область", "Вокзальна", 48.467, 35.040, 35, 12, 2, 1, 1, 1, 2, "Ключ у сейфі."),
        new("Просторі апартаменти на набережній", "Велика вітальня і два балкони з видом на річку.", ListingCategory.LargeApartments, "Дніпро", "Дніпропетровська область", "Набережна", 48.464, 35.046, 88, 22, 6, 2, 3, 2, 3, "Не сушіть білизну на балконі."),
        new("Хостел біля моря, кімната на двох", "Окремий номер у хостелі, спільна кухня й тераса.", ListingCategory.Hostels, "Одеса", "Одеська область", "Ланжерон", 46.478, 30.755, 28, 8, 2, 1, 2, 1, 2, "Кухня спільна — прибирайте за собою."),
        new("Дизайнерський будинок на Полтавщині", "Сучасний мінімалізм серед поля.", ListingCategory.Designer, "Полтава", "Полтавська область", "околиця", 49.588, 34.551, 99, 24, 4, 2, 2, 2, 4, "Тварин можна за попередньою згодою."),
        new("Камерний люкс у центрі Чернівців", "Відреставрована квартира в історичному будинку.", ListingCategory.Luxe, "Чернівці", "Чернівецька область", "Центр", 48.292, 25.935, 130, 28, 3, 1, 1, 1, 3, "Після 21:00 — тихо."),
        new("Сільська хата з піччю", "Автентичний відпочинок, мед і сад.", ListingCategory.Countryside, "Чернігів", "Чернігівська область", "с. Козелець", 50.913, 31.115, 48, 10, 4, 2, 3, 1, 2, "Піч топити лише з господарем.")
    ];

    private sealed record SeedItem(
        string Title,
        string Description,
        ListingCategory Category,
        string City,
        string Region,
        string Address,
        double Latitude,
        double Longitude,
        decimal PricePerNight,
        decimal CleaningFee,
        int MaxGuests,
        int Bedrooms,
        int Beds,
        int Bathrooms,
        int AmenityCount,
        string HouseRules);
}
