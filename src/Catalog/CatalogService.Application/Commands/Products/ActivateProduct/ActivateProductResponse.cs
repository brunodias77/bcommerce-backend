namespace CatalogService.Application.Commands.Products.ActivateProduct;

public class ActivateProductResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime UpdatedAt { get; set; }
}