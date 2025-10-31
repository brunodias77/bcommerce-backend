using BuildingBlocks.Core.Domain;
using BuildingBlocks.Core.Validations;
using CatalogService.Domain.Entities;
using CatalogService.Domain.ValueObjects;

namespace CatalogService.Domain.Aggregates;

public class ProductReview : AggregateRoot
{
    private readonly List<ReviewVote> _votes = new();

    public Guid ProductId { get; private set; }
    public Guid UserId { get; private set; }
    
    public Rating Rating { get; private set; }
    public string? Title { get; private set; }
    public string? Comment { get; private set; }
    
    public bool IsVerifiedPurchase { get; private set; }
    public int HelpfulCount { get; private set; }
    public int UnhelpfulCount { get; private set; }
    
    // Moderation
    public bool IsApproved { get; private set; }
    public bool IsFeatured { get; private set; }
    public DateTime? ModeratedAt { get; private set; }
    public Guid? ModeratedBy { get; private set; }
    
    public int Version { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    
    private ProductReview() 
    {
        Rating = Rating.OneStar;
    }

    public static ProductReview Create(
        Guid productId,
        Guid userId,
        Rating rating,
        string? title = null,
        string? comment = null,
        bool isVerifiedPurchase = false)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("ProductId cannot be empty", nameof(productId));

        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty", nameof(userId));

        if (rating == null)
            throw new ArgumentException("Rating is required", nameof(rating));

        return new ProductReview
        {
            ProductId = productId,
            UserId = userId,
            Rating = rating,
            Title = title,
            Comment = comment,
            IsVerifiedPurchase = isVerifiedPurchase,
            HelpfulCount = 0,
            UnhelpfulCount = 0,
            IsApproved = false,
            IsFeatured = false,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
    
    public override ValidationHandler Validate()
    {
        throw new NotImplementedException();
    }
}