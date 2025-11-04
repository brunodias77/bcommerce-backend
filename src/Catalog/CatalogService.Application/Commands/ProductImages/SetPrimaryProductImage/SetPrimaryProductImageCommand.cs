using BuildingBlocks.Core.Responses;
using BuildingBlocks.CQRS.Commands;

namespace CatalogService.Application.Commands.ProductImages.SetPrimaryProductImage;
/// <summary>
/// Comando para definir uma imagem como principal de um produto
/// </summary>
public class SetPrimaryProductImageCommand : ICommand<ApiResponse<bool>>
{
    /// <summary>
    /// ID do produto
    /// </summary>
    public Guid ProductId { get; set; }
    
    /// <summary>
    /// ID da imagem a ser definida como principal
    /// </summary>
    public Guid ImageId { get; set; }
}