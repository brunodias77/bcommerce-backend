using BuildingBlocks.Core.Validations;
using BuildingBlocks.CQRS.Validations;

namespace CatalogService.Application.Commands.Categories.DeleteCategory;

/// <summary>
/// Validador para o comando de deletar categoria
/// </summary>
public class DeleteCategoryCommandValidator : IValidator<DeleteCategoryCommand>
{
    /// <summary>
    /// Valida o comando de deletar categoria
    /// </summary>
    /// <param name="command">Comando a ser validado</param>
    /// <returns>Handler de validação com os erros encontrados</returns>
    public ValidationHandler Validate(DeleteCategoryCommand command)
    {
        var handler = new ValidationHandler();

        // Validar ID
        if (command.Id == Guid.Empty)
            handler.Add("ID da categoria é obrigatório");

        return handler;
    }
}