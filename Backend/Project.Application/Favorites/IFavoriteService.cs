using Project.Application.Listings;

namespace Project.Application.Favorites;

public interface IFavoriteService
{
    Task<IReadOnlyList<ListingCardDto>> ListMineAsync(string userId, CancellationToken cancellationToken = default);
    Task AddAsync(string userId, Guid listingId, CancellationToken cancellationToken = default);
    Task RemoveAsync(string userId, Guid listingId, CancellationToken cancellationToken = default);
}
