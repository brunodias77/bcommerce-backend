using BuildingBlocks.Core.Responses;
using BuildingBlocks.CQRS.Commands;

namespace CatalogService.Application.Commands.Products.ActivateProduct;

public class ActivateProductCommand : ICommand<ApiResponse<ActivateProductResponse>>
{
    public Guid ProductId { get; set; }
}