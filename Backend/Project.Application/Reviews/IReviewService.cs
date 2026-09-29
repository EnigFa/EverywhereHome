using Project.Application.Listings;

namespace Project.Application.Reviews;

public record CreateReviewRequest(decimal Rating, string Text);

public interface IReviewService
{
    Task<ReviewItemDto> CreateAsync(string userId, Guid listingId, CreateReviewRequest request, CancellationToken cancellationToken = default);
}
