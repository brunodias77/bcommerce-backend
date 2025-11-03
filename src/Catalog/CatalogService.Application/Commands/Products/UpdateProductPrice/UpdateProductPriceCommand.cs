using BuildingBlocks.Core.Responses;
using BuildingBlocks.CQRS.Commands;

namespace CatalogService.Application.Commands.Products.UpdateProductPrice;

public class UpdateProductPriceCommand : ICommand<ApiResponse<UpdateProductPriceResponse>>
{
    public Guid Id { get; set; }
    public decimal Price { get; set; }
    public decimal? CompareAtPrice { get; set; }
}