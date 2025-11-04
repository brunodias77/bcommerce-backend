using BuildingBlocks.Core.Validations;
using BuildingBlocks.CQRS.Validations;

namespace CatalogService.Application.Commands.Products.DeleteProductImage;

/// <summary>
/// Validador para o comando de deletar imagem de produto
/// </summary>
public class DeleteProductImageValidator : IValidator<DeleteProductImageCommand>
{
    /// <summary>
    /// Valida o comando de deletar imagem
    /// </summary>
    /// <param name="request">Comando a ser validado</param>
    /// <returns>Handler de validação</returns>
    public ValidationHandler Validate(DeleteProductImageCommand request)
    {
        var handler = new ValidationHandler();

        // Validar ID da imagem
        if (request.Id == Guid.Empty)
            handler.Add("ID da imagem é obrigatório");

        return handler;
    }
}