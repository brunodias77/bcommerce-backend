using BuildingBlocks.Core.Responses;
using BuildingBlocks.CQRS.Commands;

namespace CatalogService.Application.Commands.Products.DeleteProductImage;

/// <summary>
/// Comando para deletar uma imagem de produto
/// </summary>
public class DeleteProductImageCommand : ICommand<ApiResponse<bool>>
{
    /// <summary>
    /// ID da imagem a ser deletada
    /// </summary>
    public Guid Id { get; set; }
}