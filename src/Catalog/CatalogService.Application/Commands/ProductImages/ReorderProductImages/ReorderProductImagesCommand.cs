using BuildingBlocks.Core.Responses;
using BuildingBlocks.CQRS.Commands;

namespace CatalogService.Application.Commands.ProductImages.ReorderProductImages;

/// <summary>
/// Comando para reordenar as imagens de um produto
/// </summary>
public class ReorderProductImagesCommand : ICommand<ApiResponse<bool>>
{
    /// <summary>
    /// ID do produto
    /// </summary>
    public Guid ProductId { get; set; }
    
    /// <summary>
    /// Lista com ID e nova ordem das imagens
    /// </summary>
    public List<ImageOrder> ImageOrders { get; set; } = new();
}