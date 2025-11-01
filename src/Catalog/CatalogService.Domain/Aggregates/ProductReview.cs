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
    
    public override ValidationHandler Validate(ValidationHandler handler)
    {
        // Validar ProductId
        if (ProductId == Guid.Empty)
            handler.Add("ID do produto é obrigatório");
        
        // Validar UserId
        if (UserId == Guid.Empty)
            handler.Add("ID do usuário é obrigatório");
        
        // Validar Rating
        if (Rating == null)
            handler.Add("Avaliação é obrigatória");
        
        // Validar Title (opcional)
        if (!string.IsNullOrEmpty(Title))
        {
            if (string.IsNullOrWhiteSpace(Title))
                handler.Add("Título não pode conter apenas espaços em branco");
            else if (Title.Length > 100)
                handler.Add("Título deve ter no máximo 100 caracteres");
        }
        
        // Validar Comment (opcional)
        if (!string.IsNullOrEmpty(Comment))
        {
            if (string.IsNullOrWhiteSpace(Comment))
                handler.Add("Comentário não pode conter apenas espaços em branco");
            else if (Comment.Length > 2000)
                handler.Add("Comentário deve ter no máximo 2000 caracteres");
        }
        
        // Validar HelpfulCount
        if (HelpfulCount < 0)
            handler.Add("Contagem de avaliações úteis deve ser maior ou igual a zero");
        
        // Validar UnhelpfulCount
        if (UnhelpfulCount < 0)
            handler.Add("Contagem de avaliações não úteis deve ser maior ou igual a zero");
        
        // Validar regras de moderação
        if (ModeratedAt.HasValue && (!ModeratedBy.HasValue || ModeratedBy.Value == Guid.Empty))
            handler.Add("Quando a avaliação foi moderada, deve ser informado quem a moderou");
        
        if (!ModeratedAt.HasValue && ModeratedBy.HasValue)
            handler.Add("Não é possível ter um moderador sem data de moderação");
        
        return handler;
    }
}