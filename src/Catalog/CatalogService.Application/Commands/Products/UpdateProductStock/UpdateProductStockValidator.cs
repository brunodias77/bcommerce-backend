using BuildingBlocks.Core.Validations;
using BuildingBlocks.CQRS.Validations;
using CatalogService.Domain.Enums;

namespace CatalogService.Application.Commands.Products.UpdateProductStock;

public class UpdateProductStockCommandValidator : IValidator<UpdateProductStockCommand>
{
    public ValidationHandler Validate(UpdateProductStockCommand command)
    {
        var handler = new ValidationHandler();
        
        // Validar Id
        if (command.Id == Guid.Empty)
            handler.Add("ID do produto é obrigatório");
        
        // Validar Stock
        if (command.Stock < 0)
            handler.Add("Quantidade deve ser maior ou igual a zero");
        
        // Validar Operation
        if (!Enum.IsDefined(typeof(StockOperation), command.Operation))
            handler.Add("Operação de estoque inválida");
        
        return handler;
    }
}