namespace CatalogService.Application.Commands.Products.CreateProduct;

public class CreateProductResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ShortDescription { get; set; }
    
    // Pricing
    public decimal Price { get; set; }
    public string Currency { get; set; } = string.Empty;
    public decimal? CompareAtPrice { get; set; }
    public decimal? CostPrice { get; set; }
    
    // Inventory
    public int Stock { get; set; }
    public int LowStockThreshold { get; set; }
    
    // Categorization
    public Guid? CategoryId { get; set; }
    
    // SEO
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    
    // Attributes
    public decimal? WeightKg { get; set; }
    public string? Sku { get; set; }
    public string? Barcode { get; set; }
    
    // Status
    public bool IsActive { get; set; }
    public bool IsFeatured { get; set; }
    
    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}