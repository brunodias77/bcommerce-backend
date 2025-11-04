namespace CatalogService.Application.Commands.Products.DeleteProductImage;

/// <summary>
/// Resposta do comando de deletar imagem de produto
/// </summary>
public class DeleteProductImageResponse
{
    /// <summary>
    /// ID da imagem deletada
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// ID do produto ao qual a imagem pertencia
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Data e hora da exclusão
    /// </summary>
    public DateTime DeletedAt { get; set; }
}