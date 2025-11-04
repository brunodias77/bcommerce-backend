using BuildingBlocks.Core.Validations;
using BuildingBlocks.CQRS.Validations;

namespace CatalogService.Application.Commands.ProductImages.SetPrimaryProductImage;
/// <summary>
/// Validador para o comando de definir imagem principal de produto
/// </summary>
public class SetPrimaryProductImageValidator : IValidator<SetPrimaryProductImageCommand>
{
    /// <summary>
    /// Valida o comando de definir imagem principal
    /// </summary>
    /// <param name="request">Comando a ser validado</param>
    /// <returns>Handler de validação com os erros encontrados</returns>
    public ValidationHandler Validate(SetPrimaryProductImageCommand request)
    {
        var handler = new ValidationHandler();
        
        // Validar ProductId
        if (request.ProductId == Guid.Empty)
            handler.Add("ID do produto é obrigatório");
        
        // Validar ImageId
        if (request.ImageId == Guid.Empty)
            handler.Add("ID da imagem é obrigatório");
        
        return handler;
    }
}