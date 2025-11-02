namespace CatalogService.Application.Commands.Products.UpdateProduct;

public record UpdateProductResponse(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? ShortDescription,
    decimal Price,
    string Currency,
    decimal? CompareAtPrice,
    decimal? CostPrice,
    int Stock,
    int LowStockThreshold,
    Guid? CategoryId,
    string? MetaTitle,
    string? MetaDescription,
    decimal? WeightKg,
    string? Sku,
    string? Barcode,
    bool IsActive,
    bool IsFeatured,
    int Version,
    DateTime CreatedAt,
    DateTime UpdatedAt
);