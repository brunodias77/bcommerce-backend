namespace CatalogService.Application.Commands.Products.UnfeatureProduct;

public class UnfeatureProductResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsFeatured { get; set; }
    public DateTime UnfeaturedAt { get; set; }
}