using BuildingBlocks.Core.Validations;
using BuildingBlocks.CQRS.Validations;

namespace CatalogService.Application.Commands.Products.FeatureProduct;

public class FeatureProductValidator : IValidator<FeatureProductCommand>
{
    public ValidationHandler Validate(FeatureProductCommand request)
    {
        var handler = new ValidationHandler();
        
        // Validar Id
        if (request.Id == Guid.Empty)
            handler.Add("ID do produto é obrigatório");
        
        return handler;
    }
}