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
    
    public override ValidationHandler Validate()
    {
        throw new NotImplementedException();
    }
}