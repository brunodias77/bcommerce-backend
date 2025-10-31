using BuildingBlocks.Core.Domain;
using BuildingBlocks.Core.Validations;

namespace CatalogService.Domain.Entities;

public class FavoriteProduct : Entity
{
    public Guid UserId { get; private set; }
    public Guid ProductId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private FavoriteProduct() { }

    public override ValidationHandler Validate()
    {
        throw new NotImplementedException();
    }
}