namespace CatalogService.Application.Commands.Categories.UpdateCategory;

/// <summary>
/// Resposta do comando de atualização de categoria
/// </summary>
public class UpdateCategoryResponse
{
    /// <summary>
    /// ID da categoria atualizada
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Nome da categoria
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Slug único da categoria
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Descrição da categoria
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// ID da categoria pai
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    /// Indica se a categoria está ativa
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Ordem de exibição da categoria
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Versão da categoria (controle de concorrência)
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Data de criação da categoria
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Data da última atualização
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}