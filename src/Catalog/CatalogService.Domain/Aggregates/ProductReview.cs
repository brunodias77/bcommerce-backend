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
    
    public override ValidationHandler Validate()
    {
        throw new NotImplementedException();
    }
}