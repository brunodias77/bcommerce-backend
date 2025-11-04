using BuildingBlocks.Core.Validations;
using BuildingBlocks.CQRS.Validations;

namespace CatalogService.Application.Commands.Categories.ActivateCategory;

public class ActivateCategoryCommandValidator : IValidator<ActivateCategoryCommand>
{
    public ValidationHandler Validate(ActivateCategoryCommand command)
    {
        var handler = new ValidationHandler();
        
        // Validar Id
        if (command.Id == Guid.Empty)
            handler.Add("ID da categoria é obrigatório");
        
        return handler;
    }
}