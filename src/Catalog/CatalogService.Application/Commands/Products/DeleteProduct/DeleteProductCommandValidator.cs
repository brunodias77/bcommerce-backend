using BuildingBlocks.Core.Validations;
using BuildingBlocks.CQRS.Validations;

namespace CatalogService.Application.Commands.Products.DeleteProduct;

/// <summary>
/// Validador para o comando DeleteProduct
/// </summary>
public class DeleteProductCommandValidator : IValidator<DeleteProductCommand>
{
    /// <summary>
    /// Valida o comando DeleteProduct
    /// </summary>
    /// <param name="request">Comando a ser validado</param>
    /// <returns>Handler de validação com os erros encontrados</returns>
    public ValidationHandler Validate(DeleteProductCommand request)
    {
        var handler = new ValidationHandler();

        // Validar ID
        if (request.Id == Guid.Empty)
            handler.Add("ID do produto é obrigatório");

        return handler;
    }
}