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

    public static ProductImage Create(
        Guid productId, 
        string url, 
        string? thumbnailUrl = null, 
        string? altText = null, 
        int displayOrder = 0, 
        bool isPrimary = false)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("ProductId cannot be empty", nameof(productId));

        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("Url is required", nameof(url));

        if (displayOrder < 0)
            throw new ArgumentException("DisplayOrder cannot be negative", nameof(displayOrder));

        return new ProductImage
        {
            ProductId = productId,
            Url = url,
            ThumbnailUrl = thumbnailUrl,
            AltText = altText,
            DisplayOrder = displayOrder,
            IsPrimary = isPrimary,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public override ValidationHandler Validate()
    {
        throw new NotImplementedException();
    }
}