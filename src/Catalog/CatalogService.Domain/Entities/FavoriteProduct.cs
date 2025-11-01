using BuildingBlocks.Core.Domain;
using BuildingBlocks.Core.Validations;

namespace CatalogService.Domain.Entities;

public class FavoriteProduct : Entity
{
    public Guid UserId { get; private set; }
    public Guid ProductId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private FavoriteProduct() { }

    public static FavoriteProduct Create(Guid userId, Guid productId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty", nameof(userId));

        if (productId == Guid.Empty)
            throw new ArgumentException("ProductId cannot be empty", nameof(productId));

        var favorite = new FavoriteProduct
        {
            UserId = userId,
            ProductId = productId,
            CreatedAt = DateTime.UtcNow
        };

        var validationResult = favorite.Validate();
        if (validationResult.HasErrors)
        {
            throw new ArgumentException($"Dados inválidos: {string.Join(", ", validationResult.Errors.Select(e => e.Message))}");
        }

        return favorite;
    }

    public override ValidationHandler Validate()
    {
        var handler = new ValidationHandler();
        
        // Validar UserId
        if (UserId == Guid.Empty)
            handler.Add("ID do usuário é obrigatório");
        
        // Validar ProductId
        if (ProductId == Guid.Empty)
            handler.Add("ID do produto é obrigatório");
        
        // Validar CreatedAt
        if (CreatedAt == default(DateTime))
            handler.Add("Data de criação é obrigatória");
        else if (CreatedAt > DateTime.UtcNow.AddMinutes(1)) // Pequena tolerância para diferenças de clock
            handler.Add("Data de criação não pode ser no futuro");
        
        return handler;
    }
}