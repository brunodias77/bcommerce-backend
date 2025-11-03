using BuildingBlocks.Core.Responses;
using BuildingBlocks.CQRS.Commands;

namespace CatalogService.Application.Commands.Products.DeactivateProduct;

public class DeactivateProductCommand : ICommand<ApiResponse<DeactivateProductResponse>>
{
    public Guid ProductId { get; set; }
}