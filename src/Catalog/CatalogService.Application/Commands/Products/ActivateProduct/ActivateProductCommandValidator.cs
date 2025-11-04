using BuildingBlocks.Core.Validations;
using BuildingBlocks.CQRS.Validations;

namespace CatalogService.Application.Commands.Products.ActivateProduct;

public class ActivateProductCommandValidator : IValidator<ActivateProductCommand>
{
    public ValidationHandler Validate(ActivateProductCommand request)
    {
        var handler = new ValidationHandler();
        
        // Validar ProductId
        if (request.ProductId == Guid.Empty)
            handler.Add("ID do produto é obrigatório");
        
        return handler;
    }
}