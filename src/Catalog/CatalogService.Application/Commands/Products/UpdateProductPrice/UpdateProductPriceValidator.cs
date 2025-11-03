using BuildingBlocks.Core.Validations;
using BuildingBlocks.CQRS.Validations;

namespace CatalogService.Application.Commands.Products.UpdateProductPrice;

public class UpdateProductPriceCommandValidator : IValidator<UpdateProductPriceCommand>
{
    public ValidationHandler Validate(UpdateProductPriceCommand command)
    {
        var handler = new ValidationHandler();
        
        // Validar Id
        if (command.Id == Guid.Empty)
            handler.Add("ID do produto é obrigatório");
        
        // Validar Price
        if (command.Price <= 0)
            handler.Add("Preço deve ser maior que zero");
        
        // Validar CompareAtPrice (opcional)
        if (command.CompareAtPrice.HasValue && command.CompareAtPrice.Value <= command.Price)
            handler.Add("Preço de comparação deve ser maior que o preço do produto");
        
        return handler;
    }
}