using BuildingBlocks.Core.Validations;
using BuildingBlocks.CQRS.Validations;

namespace CatalogService.Application.Commands.Products.DeactivateProduct;

public class DeactivateProductCommandValidator : IValidator<DeactivateProductCommand>
{
    public ValidationHandler Validate(DeactivateProductCommand request)
    {
        var handler = new ValidationHandler();
        
        // Validar ProductId
        if (request.ProductId == Guid.Empty)
            handler.Add("ID do produto é obrigatório");
        
        return handler;
    }
}