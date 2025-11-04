using BuildingBlocks.Core.Responses;
using BuildingBlocks.CQRS.Commands;

namespace CatalogService.Application.Commands.Products.DeleteProduct;

/// <summary>
/// Comando para deletar um produto (soft delete)
/// </summary>
public class DeleteProductCommand : ICommand<ApiResponse<bool>>
{
    /// <summary>
    /// ID do produto a ser deletado
    /// </summary>
    public Guid Id { get; set; }
}