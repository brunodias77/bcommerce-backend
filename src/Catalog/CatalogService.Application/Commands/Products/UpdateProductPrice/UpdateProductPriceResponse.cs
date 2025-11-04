namespace CatalogService.Application.Commands.Products.UpdateProductPrice;

public record UpdateProductPriceResponse(
    Guid Id,
    string Name,
    decimal Price,
    decimal? CompareAtPrice,
    DateTime UpdatedAt
);