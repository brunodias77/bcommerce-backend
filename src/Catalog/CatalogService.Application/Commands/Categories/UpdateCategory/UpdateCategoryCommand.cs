using BuildingBlocks.Core.Responses;
using BuildingBlocks.CQRS.Commands;

namespace CatalogService.Application.Commands.Categories.UpdateCategory;

/// <summary>
/// Command para atualizar uma categoria existente
/// </summary>
public class UpdateCategoryCommand : ICommand<ApiResponse<UpdateCategoryResponse>>
{
    /// <summary>
    /// ID da categoria a ser atualizada
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Nome da categoria
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Slug único da categoria (URL-friendly)
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Descrição da categoria (opcional)
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// ID da categoria pai (opcional para subcategorias)
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    /// Ordem de exibição da categoria
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Indica se a categoria está ativa
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Metadados adicionais em formato JSON
    /// </summary>
    public string Metadata { get; set; } = "{}";
}