using BuildingBlocks.Core.Responses;
using BuildingBlocks.CQRS.Commands;

namespace CatalogService.Application.Commands.Products.UnfeatureProduct;

public class UnfeatureProductCommand : ICommand<ApiResponse<UnfeatureProductResponse>>
{
    public Guid Id { get; set; }
}