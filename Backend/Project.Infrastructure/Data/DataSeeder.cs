using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Project.Application.Entities;

namespace Project.Infrastructure.Data;

public static class DataSeeder
{
    private const string HostPassword = "Host123!";
    private const string GuestPassword = "Guest123!";
    private const string AdminPassword = "Admin123!";

    public static async Task SeedAsync(AppDbContext db, UserManager<AppUser> users)
    {
        await EnsureChiefAsync(users);
        var ready = await db.Listings.CountAsync() >= 100
            && await db.ListingPhotos.AnyAsync()
            && !await db.ListingPhotos.AnyAsync(p => p.Url.StartsWith("http"));
        if (ready)
        {
            return;
        }

        await ClearAsync(db);
        await FillAsync(db, users);
    }

    private static async Task ClearAsync(AppDbContext db)
    {
        await db.ChatMessages.ExecuteDeleteAsync();
        await db.ConversationReads.ExecuteDeleteAsync();
        await db.Conversations.ExecuteDeleteAsync();
        await db.Reports.ExecuteDeleteAsync();
        await db.Payments.ExecuteDeleteAsync();
        await db.Reviews.ExecuteDeleteAsync();
        await db.Favorites.ExecuteDeleteAsync();
        await db.Bookings.ExecuteDeleteAsync();
        await db.ListingPhotos.ExecuteDeleteAsync();
        await db.ListingAmenities.ExecuteDeleteAsync();
        await db.ListingCategoryLinks.ExecuteDeleteAsync();
        await db.Listings.ExecuteDeleteAsync();
        await db.HostApplications.ExecuteDeleteAsync();
        await db.Amenities.ExecuteDeleteAsync();
        await db.Set<IdentityUserLogin<string>>().ExecuteDeleteAsync();
        await db.Set<IdentityUserToken<string>>().ExecuteDeleteAsync();
        await db.Set<IdentityUserClaim<string>>().ExecuteDeleteAsync();
        await db.Set<IdentityUserRole<string>>().ExecuteDeleteAsync();
        await db.Users.ExecuteDeleteAsync();
    }

    private static async Task FillAsync(AppDbContext db, UserManager<AppUser> users)
    {
        var admin = await CreateUser(users, "admin@everywherehome.local", "Адміністратор", AdminPassword, host: false, admin: true, trust: 1, chief: true);
        var ilona = await CreateUser(users, "host@everywherehome.local", "Ілона Коваль", HostPassword, host: true, admin: false, trust: 1);
        var olena = await CreateUser(users, "olena@everywherehome.local", "Олена Шевченко", HostPassword, host: true, admin: false, trust: 1);
        var taras = await CreateUser(users, "taras@everywherehome.local", "Тарас Мельник", HostPassword, host: true, admin: false, trust: 0);
        var sofia = await CreateUser(users, "sofia@everywherehome.local", "Софія Бондар", HostPassword, host: true, admin: false, trust: 0);
        var andriy = await CreateUser(users, "andriy@everywherehome.local", "Андрій Кравченко", HostPassword, host: true, admin: false, trust: 2);
        var mariia = await CreateUser(users, "mariia@everywherehome.local", "Марія Ткаченко", HostPassword, host: true, admin: false, trust: 1);
        var hosts = new[] { ilona, olena, taras, sofia, andriy, mariia };

        var marko = await CreateUser(users, "guest@everywherehome.local", "Марко Гість", GuestPassword, host: false, admin: false, trust: 0);
        var oksana = await CreateUser(users, "oksana@everywherehome.local", "Оксана Лисенко", GuestPassword, host: false, admin: false, trust: 0);
        var denys = await CreateUser(users, "denys@everywherehome.local", "Денис Романюк", GuestPassword, host: false, admin: false, trust: 2);
        var yulia = await CreateUser(users, "yulia@everywherehome.local", "Юлія Петренко", GuestPassword, host: false, admin: false, trust: 1);
        var guests = new[] { marko, oksana, denys, yulia };

        var amenities = new[]
        {
            await Amenity(db, "Wi-Fi"),
            await Amenity(db, "Кухня"),
            await Amenity(db, "Паркінг"),
            await Amenity(db, "Кондиціонер"),
            await Amenity(db, "Пральна машина"),
            await Amenity(db, "Балкон")
        };
        await db.SaveChangesAsync();

        var listings = BuildListings(hosts, amenities);
        db.Listings.AddRange(listings);
        await db.SaveChangesAsync();

        await SeedBookingsAndReviewsAsync(db, listings, guests);
        await SeedMessagesAsync(db, listings, hosts, guests, admin);
        await SeedApplicationsAsync(db, hosts, oksana, denys, admin);
    }

    private static async Task EnsureChiefAsync(UserManager<AppUser> users)
    {
        var admin = await users.FindByEmailAsync("admin@everywherehome.local");
        if (admin is null || (admin.IsChiefAdmin && admin.IsAdmin))
        {
            return;
        }

        admin.IsAdmin = true;
        admin.IsChiefAdmin = true;
        await users.UpdateAsync(admin);
    }

    private static async Task<AppUser> CreateUser(UserManager<AppUser> users, string email, string name, string password, bool host, bool admin, int trust, bool chief = false)
    {
        var user = new AppUser
        {
            UserName = email,
            Email = email,
            DisplayName = name,
            IsHost = host,
            IsAdmin = admin,
            IsChiefAdmin = chief,
            EmailConfirmed = true,
            TrustLevel = trust,
            Hometown = host ? "Україна" : "Київ",
            Intro = host ? "Приймаю гостей і стежу, щоб оселя була охайною." : "Шукаю житло для коротких поїздок."
        };
        var result = await users.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        return user;
    }

    private static async Task<Amenity> Amenity(AppDbContext db, string name)
    {
        var amenity = new Amenity { Id = Guid.NewGuid(), Name = name };
        db.Amenities.Add(amenity);
        return amenity;
    }

    private static List<Listing> BuildListings(AppUser[] hosts, Amenity[] amenities)
    {
        var cities = new (string City, string Region, double Lat, double Lon)[]
        {
            ("Київ", "Київська область", 50.4501, 30.5234),
            ("Львів", "Львівська область", 49.8397, 24.0297),
            ("Одеса", "Одеська область", 46.4825, 30.7233),
            ("Харків", "Харківська область", 49.9935, 36.2304),
            ("Дніпро", "Дніпропетровська область", 48.4647, 35.0462),
            ("Вінниця", "Вінницька область", 49.2331, 28.4682),
            ("Чернівці", "Чернівецька область", 48.2921, 25.9358),
            ("Яремче", "Івано-Франківська область", 48.4536, 24.5564),
            ("Полтава", "Полтавська область", 49.5883, 34.5514),
            ("Чернігів", "Чернігівська область", 51.4982, 31.2893),
            ("Ужгород", "Закарпатська область", 48.6208, 22.2879),
            ("Буковель", "Івано-Франківська область", 48.3572, 24.4014)
        };
        var templates = new (string Title, string Text, ListingCategory Category, int Guests, int Rooms, int Beds, int Baths, int Price)[]
        {
            ("Затишна студія", "Компактна студія для одного або пари, з робочим місцем.", ListingCategory.SmallApartments, 2, 0, 1, 1, 42),
            ("Квартира в центрі", "Житло в пішій доступності від центру і кафе.", ListingCategory.CityCenter, 3, 1, 2, 1, 78),
            ("Просторі апартаменти", "Окрема спальня і вітальня, зручно для компанії.", ListingCategory.LargeApartments, 5, 2, 3, 1, 110),
            ("Ліжко в хостелі", "Акуратне місце в спільній кімнаті, кухня на поверсі.", ListingCategory.Hostels, 1, 1, 1, 1, 18),
            ("Будинок з краєвидом", "Окремий будинок, тиша і вид з тераси.", ListingCategory.ScenicViews, 6, 3, 4, 2, 140),
            ("Сільська садиба", "Дім з подвір'ям, підходить для спокійних вихідних.", ListingCategory.Countryside, 6, 2, 4, 1, 85),
            ("Дизайнерський лофт", "Світлий інтер'єр, високі стелі і нова кухня.", ListingCategory.Designer, 3, 1, 1, 1, 120),
            ("Luxe резиденція", "Просторий дім з великою вітальнею для особливої поїздки.", ListingCategory.Luxe, 8, 4, 5, 3, 240),
            ("Кімната біля парку", "Окрема кімната в тихій квартирі поруч із зеленою зоною.", ListingCategory.SmallApartments, 2, 1, 1, 1, 36)
        };
        var notes = new[] { "з балконом", "біля парку", "для сім'ї", "з видом на подвір'я", "тихий поверх", "світла кухня" };
        var rules = new[] { "Тиша після 22:00.", "Без вечірок.", "Не палити в приміщенні.", "Тварини за домовленістю." };

        var listings = new List<Listing>();
        for (var i = 0; i < 108; i++)
        {
            var city = cities[i % cities.Length];
            var template = templates[i % templates.Length];
            var host = hosts[i % hosts.Length];
            var note = notes[i % notes.Length];
            var listing = new Listing
            {
                Id = Guid.NewGuid(),
                HostId = host.Id,
                Title = $"{template.Title} {note}, {city.City}",
                Description = $"{template.Text} Господар: {host.DisplayName}. {note}.",
                Category = template.Category,
                City = city.City,
                Region = city.Region,
                Country = "Україна",
                Address = $"вул. Центральна, {12 + (i % 80)}",
                Latitude = city.Lat + (i % 7) * 0.01,
                Longitude = city.Lon + (i % 5) * 0.01,
                PricePerNight = template.Price + (i % 9) * 5,
                CleaningFee = 10 + (i % 4) * 5,
                MaxGuests = template.Guests,
                Bedrooms = template.Rooms,
                Beds = template.Beds,
                Bathrooms = template.Baths,
                HouseRules = rules[i % rules.Length],
                SafetyRules = "Детектор диму та аптечка на місці.",
                CancellationPolicy = i % 3 == 0 ? "Безкоштовне скасування за 5 днів." : "Гнучке скасування за 2 дні.",
                IsPublished = i % 17 != 0,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-(i % 50))
            };
            listing.CategoryLinks.Add(new ListingCategoryLink { ListingId = listing.Id, Category = template.Category });
            if (i % 4 == 0)
            {
                var extra = (ListingCategory)((int)(template.Category + 1) % 8);
                listing.CategoryLinks.Add(new ListingCategoryLink { ListingId = listing.Id, Category = extra });
            }

            for (var photo = 0; photo < 3; photo++)
            {
                var index = (i + photo * 5) % 18 + 1;
                listing.Photos.Add(new ListingPhoto
                {
                    Id = Guid.NewGuid(),
                    Url = $"/seed/{index:D2}.jpg",
                    SortOrder = photo
                });
            }

            for (var amenity = 0; amenity < 3 + (i % 3); amenity++)
            {
                listing.Amenities.Add(new ListingAmenity { AmenityId = amenities[(i + amenity) % amenities.Length].Id });
            }

            listings.Add(listing);
        }

        return listings;
    }

    private static async Task SeedBookingsAndReviewsAsync(AppDbContext db, List<Listing> listings, AppUser[] guests)
    {
        var published = listings.Where(l => l.IsPublished).Take(12).ToList();
        for (var i = 0; i < published.Count; i++)
        {
            var guest = guests[i % guests.Length];
            var listing = published[i];
            var checkIn = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-20 - i));
            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                ListingId = listing.Id,
                GuestId = guest.Id,
                CheckIn = checkIn,
                CheckOut = checkIn.AddDays(2 + (i % 3)),
                GuestsCount = Math.Min(2, listing.MaxGuests),
                PaymentPlan = PaymentPlan.Full,
                TotalAmount = listing.PricePerNight * 2 + listing.CleaningFee,
                Status = i % 4 == 0 ? BookingStatus.Confirmed : BookingStatus.Completed,
                MessageToHost = "Будемо ввечері.",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-30 - i)
            };
            db.Bookings.Add(booking);
            if (booking.Status == BookingStatus.Completed)
            {
                db.Reviews.Add(new Review
                {
                    Id = Guid.NewGuid(),
                    ListingId = listing.Id,
                    AuthorId = guest.Id,
                    BookingId = booking.Id,
                    Rating = 4 + (i % 2),
                    Text = i % 2 == 0 ? "Чисто, тихо, господар на зв'язку." : "Зручне розташування, зупинились би ще.",
                    CreatedAtUtc = booking.CheckOut.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
                });
            }

            if (i < 4)
            {
                db.Favorites.Add(new Favorite { UserId = guest.Id, ListingId = listing.Id });
            }
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedMessagesAsync(AppDbContext db, List<Listing> listings, AppUser[] hosts, AppUser[] guests, AppUser admin)
    {
        var published = listings.Where(l => l.IsPublished).ToList();
        var samples = new (AppUser Guest, int ListingIndex, string[] Lines, bool HostRead)[]
        {
            (guests[0], 0, ["Добрий день, чи вільні дати на наступні вихідні?", "Так, ці дати вільні. Заїзд після 15:00.", "Дякую, тоді бронюю."], true),
            (guests[1], 3, ["Чи є паркінг біля будинку?", "Так, одне місце у дворі."], false),
            (guests[2], 7, ["Можна з невеликим собакою?", "Так, за попередньою домовленістю."], true),
            (guests[3], 11, ["О котрій можна забрати ключі?", "Напишіть за годину до прибуття."], false)
        };

        foreach (var sample in samples)
        {
            var listing = published[sample.ListingIndex];
            var host = hosts.First(h => h.Id == listing.HostId);
            var conversation = Conversation(ConversationKind.Listing, sample.Guest.Id, host.Id, listing.Id, CaseStatus.New);
            db.Conversations.Add(conversation);
            AddThread(db, conversation, sample.Guest.Id, host.Id, sample.Lines);
            db.ConversationReads.Add(new ConversationRead { ConversationId = conversation.Id, UserId = sample.Guest.Id, LastReadAtUtc = DateTime.UtcNow });
            if (sample.HostRead)
            {
                db.ConversationReads.Add(new ConversationRead { ConversationId = conversation.Id, UserId = host.Id, LastReadAtUtc = DateTime.UtcNow });
            }
        }

        var direct = Conversation(ConversationKind.Direct, guests[0].Id, hosts[1].Id, null, CaseStatus.New);
        db.Conversations.Add(direct);
        AddThread(db, direct, guests[0].Id, hosts[1].Id, ["Добрий день, хотів уточнити деталі поза оголошенням.", "Звісно, слухаю."]);
        db.ConversationReads.Add(new ConversationRead { ConversationId = direct.Id, UserId = guests[0].Id, LastReadAtUtc = DateTime.UtcNow });

        var supportNew = Conversation(ConversationKind.Support, guests[1].Id, null, null, CaseStatus.New);
        db.Conversations.Add(supportNew);
        AddThread(db, supportNew, guests[1].Id, admin.Id, ["Не можу знайти лист підтвердження бронювання."]);

        var supportWork = Conversation(ConversationKind.Support, guests[2].Id, null, null, CaseStatus.InProgress);
        supportWork.AssigneeId = admin.Id;
        db.Conversations.Add(supportWork);
        AddThread(db, supportWork, guests[2].Id, admin.Id, ["Оплата зависла на підтвердженні.", "Перевіряю ваше бронювання, напишіть номер, якщо є."]);
        db.ConversationReads.Add(new ConversationRead { ConversationId = supportWork.Id, UserId = admin.Id, LastReadAtUtc = DateTime.UtcNow.AddMinutes(-30) });

        var supportDone = Conversation(ConversationKind.Support, guests[3].Id, null, null, CaseStatus.Resolved);
        supportDone.AssigneeId = admin.Id;
        supportDone.ResolvedById = admin.Id;
        supportDone.ResolvedAtUtc = DateTime.UtcNow.AddDays(-2);
        supportDone.Decision = "Доступ до бронювання відновлено, лист надіслано повторно.";
        db.Conversations.Add(supportDone);
        AddThread(db, supportDone, guests[3].Id, admin.Id, ["Не відкривається сторінка оплати.", "Виправили посилання. Спробуйте ще раз."]);

        var listingForReport = published[5];
        var newReport = new Report
        {
            Id = Guid.NewGuid(),
            ReporterId = guests[0].Id,
            Target = ReportTarget.Listing,
            ListingId = listingForReport.Id,
            Text = "На фото інше планування, ніж у описі.",
            Status = ReportStatus.New,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-1)
        };
        var workReportConversation = Conversation(ConversationKind.Report, guests[1].Id, hosts.First(h => h.Id == published[8].HostId).Id, published[8].Id, CaseStatus.InProgress);
        workReportConversation.AssigneeId = admin.Id;
        db.Conversations.Add(workReportConversation);
        AddThread(db, workReportConversation, guests[1].Id, admin.Id, ["Господар не відповідає після оплати.", "Взяли звернення в роботу, напишемо рішення сьогодні."]);
        var workReport = new Report
        {
            Id = Guid.NewGuid(),
            ReporterId = guests[1].Id,
            Target = ReportTarget.Host,
            ReportedUserId = published[8].HostId,
            ListingId = published[8].Id,
            Text = "Після підтвердження броні господар перестав відповідати.",
            Status = ReportStatus.InProgress,
            AssigneeId = admin.Id,
            ConversationId = workReportConversation.Id,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-3)
        };
        workReportConversation.ReportId = workReport.Id;
        var doneReport = new Report
        {
            Id = Guid.NewGuid(),
            ReporterId = guests[3].Id,
            Target = ReportTarget.Listing,
            ListingId = published[2].Id,
            Text = "У правилах не було згадано про шумний двір.",
            Status = ReportStatus.Resolved,
            AssigneeId = admin.Id,
            ResolvedById = admin.Id,
            ResolvedAtUtc = DateTime.UtcNow.AddDays(-4),
            Decision = "Опис доповнено, оголошення лишається опублікованим.",
            CreatedAtUtc = DateTime.UtcNow.AddDays(-6)
        };
        db.Reports.AddRange(newReport, workReport, doneReport);
        await db.SaveChangesAsync();
    }

    private static async Task SeedApplicationsAsync(AppDbContext db, AppUser[] hosts, AppUser pending, AppUser rejected, AppUser admin)
    {
        foreach (var host in hosts)
        {
            db.HostApplications.Add(new HostApplication
            {
                Id = Guid.NewGuid(),
                UserId = host.Id,
                FullName = host.DisplayName,
                DocumentUrl = "/seed/01.jpg",
                Status = HostApplicationStatus.Approved,
                AdminNote = "Документ перевірено.",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-40)
            });
        }

        db.HostApplications.Add(new HostApplication
        {
            Id = Guid.NewGuid(),
            UserId = pending.Id,
            FullName = pending.DisplayName,
            DocumentUrl = "/seed/02.jpg",
            Status = HostApplicationStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-1)
        });
        db.HostApplications.Add(new HostApplication
        {
            Id = Guid.NewGuid(),
            UserId = rejected.Id,
            FullName = rejected.DisplayName,
            DocumentUrl = "/seed/03.jpg",
            Status = HostApplicationStatus.Rejected,
            AdminNote = "На фото документа не читається прізвище.",
            CreatedAtUtc = DateTime.UtcNow.AddDays(-8)
        });
        await db.SaveChangesAsync();
        _ = admin;
    }

    private static Conversation Conversation(ConversationKind kind, string userId, string? hostId, Guid? listingId, CaseStatus status) => new()
    {
        Id = Guid.NewGuid(),
        Kind = kind,
        UserId = userId,
        HostId = hostId,
        ListingId = listingId,
        Status = status,
        CreatedAtUtc = DateTime.UtcNow.AddDays(-2)
    };

    private static void AddThread(AppDbContext db, Conversation conversation, string firstSender, string secondSender, string[] lines)
    {
        for (var i = 0; i < lines.Length; i++)
        {
            db.ChatMessages.Add(new ChatMessage
            {
                Id = Guid.NewGuid(),
                ConversationId = conversation.Id,
                SenderId = i % 2 == 0 ? firstSender : secondSender,
                Text = lines[i],
                CreatedAtUtc = conversation.CreatedAtUtc.AddMinutes(10 * (i + 1))
            });
        }
    }
}
