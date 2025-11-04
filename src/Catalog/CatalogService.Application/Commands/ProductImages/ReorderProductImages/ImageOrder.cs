namespace CatalogService.Application.Commands.ProductImages.ReorderProductImages;

/// <summary>
/// DTO para representar a nova ordem de uma imagem
/// </summary>
public class ImageOrder
{
    /// <summary>
    /// ID da imagem
    /// </summary>
    public Guid ImageId { get; set; }
    
    /// <summary>
    /// Nova ordem de exibição da imagem
    /// </summary>
    public int DisplayOrder { get; set; }
}