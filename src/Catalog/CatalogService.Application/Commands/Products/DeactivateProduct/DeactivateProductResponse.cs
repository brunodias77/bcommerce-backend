namespace CatalogService.Application.Commands.Products.DeactivateProduct;

public class DeactivateProductResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime UpdatedAt { get; set; }
}