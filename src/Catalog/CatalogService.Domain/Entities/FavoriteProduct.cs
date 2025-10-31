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

        return new FavoriteProduct
        {
            UserId = userId,
            ProductId = productId,
            CreatedAt = DateTime.UtcNow
        };
    }

    public override ValidationHandler Validate()
    {
        throw new NotImplementedException();
    }
}