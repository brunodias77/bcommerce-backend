using BuildingBlocks.Core.Validations;
using BuildingBlocks.CQRS.Validations;

namespace CatalogService.Application.Commands.Products.UpdateProductImage;

public class UpdateProductImageValidator : IValidator<UpdateProductImageCommand>
{
    public ValidationHandler Validate(UpdateProductImageCommand request)
    {
        var handler = new ValidationHandler();
        
        // Validar Id
        if (request.Id == Guid.Empty)
            handler.Add("ID da imagem é obrigatório");
        
        // Validar ProductId
        if (request.ProductId == Guid.Empty)
            handler.Add("ID do produto é obrigatório");
        
        // Validar Url
        if (string.IsNullOrWhiteSpace(request.Url))
            handler.Add("URL da imagem é obrigatória");
        else if (!Uri.TryCreate(request.Url, UriKind.Absolute, out _))
            handler.Add("URL da imagem deve ter formato válido");
        
        // Validar ThumbnailUrl se fornecida
        if (!string.IsNullOrWhiteSpace(request.ThumbnailUrl) && 
            !Uri.TryCreate(request.ThumbnailUrl, UriKind.Absolute, out _))
            handler.Add("URL da miniatura deve ter formato válido");
        
        // Validar AltText se fornecido
        if (!string.IsNullOrWhiteSpace(request.AltText) && request.AltText.Length > 500)
            handler.Add("Texto alternativo não pode exceder 500 caracteres");
        
        // Validar DisplayOrder
        if (request.DisplayOrder < 0)
            handler.Add("Ordem de exibição deve ser maior ou igual a zero");
        
        return handler;
    }
}