using BuildingBlocks.Core.Responses;
using BuildingBlocks.CQRS.Commands;
using CatalogService.Domain.Enums;

namespace CatalogService.Application.Commands.Products.UpdateProductStock;

public class UpdateProductStockCommand : ICommand<ApiResponse<UpdateProductStockResponse>>
{
    public Guid Id { get; set; }
    public int Stock { get; set; }
    public StockOperation Operation { get; set; }
}