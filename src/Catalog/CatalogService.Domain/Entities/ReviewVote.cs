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

        var reviewVote = new ReviewVote
        {
            ReviewId = reviewId,
            UserId = userId,
            IsHelpful = isHelpful,
            CreatedAt = DateTime.UtcNow
        };

        var validationResult = reviewVote.Validate();
        if (validationResult.HasErrors)
        {
            throw new ArgumentException($"Dados inválidos: {string.Join(", ", validationResult.Errors.Select(e => e.Message))}");
        }

        return reviewVote;
    }
    
    public override ValidationHandler Validate()
    {
        var handler = new ValidationHandler();
        
        // Validar ReviewId
        if (ReviewId == Guid.Empty)
            handler.Add("ID da avaliação é obrigatório");
        
        // Validar UserId
        if (UserId == Guid.Empty)
            handler.Add("ID do usuário é obrigatório");
        
        // Validar CreatedAt
        if (CreatedAt == default(DateTime))
            handler.Add("Data de criação é obrigatória");
        else if (CreatedAt > DateTime.UtcNow.AddMinutes(1))
            handler.Add("Data de criação não pode estar no futuro");
        
        return handler;
    }
}