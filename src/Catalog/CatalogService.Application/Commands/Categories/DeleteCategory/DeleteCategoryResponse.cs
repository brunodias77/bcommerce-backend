namespace CatalogService.Application.Commands.Categories.DeleteCategory;

/// <summary>
/// Resposta do comando de deletar categoria
/// </summary>
public class DeleteCategoryResponse
{
    /// <summary>
    /// Indica se a categoria foi deletada com sucesso
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// ID da categoria deletada
    /// </summary>
    public Guid CategoryId { get; set; }

    /// <summary>
    /// Mensagem de confirmação
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Construtor padrão
    /// </summary>
    public DeleteCategoryResponse() { }

    /// <summary>
    /// Construtor com parâmetros
    /// </summary>
    /// <param name="success">Sucesso da operação</param>
    /// <param name="categoryId">ID da categoria</param>
    /// <param name="message">Mensagem</param>
    public DeleteCategoryResponse(bool success, Guid categoryId, string message = "")
    {
        Success = success;
        CategoryId = categoryId;
        Message = message;
    }
}