using BuildingBlocks.Core.Validations;
using BuildingBlocks.CQRS.Validations;

namespace CatalogService.Application.Commands.FavoriteProducts.AddProductToFavorites;

public class AddProductToFavoritesCommandValidator : IValidator<AddProductToFavoritesCommand>
{
    public ValidationHandler Validate(AddProductToFavoritesCommand request)
    {
        var handler = new ValidationHandler();
        
        // Validar UserId
        if (request.UserId == Guid.Empty)
            handler.Add("ID do usuário é obrigatório");
        
        // Validar ProductId
        if (request.ProductId == Guid.Empty)
            handler.Add("ID do produto é obrigatório");
        
        return handler;
    }
}