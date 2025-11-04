using BuildingBlocks.Core.Responses;
using BuildingBlocks.CQRS.Commands;

namespace CatalogService.Application.Commands.FavoriteProducts.AddProductToFavorites;

public class AddProductToFavoritesCommand : ICommand<ApiResponse<bool>>
{
    public Guid UserId { get; set; }
    public Guid ProductId { get; set; }
}