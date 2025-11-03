using BuildingBlocks.Core.Responses;
using BuildingBlocks.CQRS.Commands;

namespace CatalogService.Application.Commands.Products.FeatureProduct;

public class FeatureProductCommand : ICommand<ApiResponse<FeatureProductResponse>>
{
    public Guid Id { get; set; }
}