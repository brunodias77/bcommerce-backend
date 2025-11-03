namespace CatalogService.Application.Commands.Products.UpdateProductStock;

public record UpdateProductStockResponse(
    Guid Id,
    string Name,
    int Stock,
    int AvailableStock,
    DateTime UpdatedAt
);