using BuildingBlocks.Core.Domain;
using BuildingBlocks.Core.Validations;

namespace CatalogService.Domain.Entities;

public class ReviewVote : Entity
{
    public Guid ReviewId { get; private set; }
    public Guid UserId { get; private set; }
    public bool IsHelpful { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    private ReviewVote() { }

    public static ReviewVote Create(Guid reviewId, Guid userId, bool isHelpful)
    {
        if (reviewId == Guid.Empty)
            throw new ArgumentException("ReviewId cannot be empty", nameof(reviewId));

        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty", nameof(userId));

        return new ReviewVote
        {
            ReviewId = reviewId,
            UserId = userId,
            IsHelpful = isHelpful,
            CreatedAt = DateTime.UtcNow
        };
    }
    
    public override ValidationHandler Validate()
    {
        throw new NotImplementedException();
    }
}