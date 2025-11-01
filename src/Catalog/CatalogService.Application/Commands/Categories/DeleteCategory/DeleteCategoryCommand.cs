using BuildingBlocks.Core.Responses;
using BuildingBlocks.CQRS.Commands;

namespace CatalogService.Application.Commands.Categories.DeleteCategory;

/// <summary>
/// Comando para deletar uma categoria (soft delete)
/// </summary>
public class DeleteCategoryCommand : ICommand<ApiResponse<DeleteCategoryResponse>>
{
    /// <summary>
    /// ID da categoria a ser deletada
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Construtor padrão
    /// </summary>
    public DeleteCategoryCommand() { }

    /// <summary>
    /// Construtor com parâmetros
    /// </summary>
    /// <param name="id">ID da categoria</param>
    public DeleteCategoryCommand(Guid id)
    {
        Id = id;
    }
}