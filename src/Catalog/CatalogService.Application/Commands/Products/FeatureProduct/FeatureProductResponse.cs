namespace CatalogService.Application.Commands.Products.FeatureProduct;

public class FeatureProductResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsFeatured { get; set; }
    public DateTime FeaturedAt { get; set; }
}