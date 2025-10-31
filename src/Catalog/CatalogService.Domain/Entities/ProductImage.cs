using BuildingBlocks.Core.Domain;
using BuildingBlocks.Core.Validations;

namespace CatalogService.Domain.Entities;

public class ProductImage : Entity
{
    public Guid ProductId { get; private set; }
    public string Url { get; private set; }
    public string? ThumbnailUrl { get; private set; }
    public string? AltText { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsPrimary { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private ProductImage() 
    {
        Url = string.Empty;
    }

    public override ValidationHandler Validate()
    {
        throw new NotImplementedException();
    }
}