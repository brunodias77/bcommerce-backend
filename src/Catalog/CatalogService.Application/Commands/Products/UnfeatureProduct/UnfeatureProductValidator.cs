using BuildingBlocks.Core.Validations;
using BuildingBlocks.CQRS.Validations;

namespace CatalogService.Application.Commands.Products.UnfeatureProduct;

public class UnfeatureProductValidator : IValidator<UnfeatureProductCommand>
{
    public ValidationHandler Validate(UnfeatureProductCommand request)
    {
        var handler = new ValidationHandler();
        
        // Validar Id
        if (request.Id == Guid.Empty)
            handler.Add("ID do produto é obrigatório");
        
        return handler;
    }
}