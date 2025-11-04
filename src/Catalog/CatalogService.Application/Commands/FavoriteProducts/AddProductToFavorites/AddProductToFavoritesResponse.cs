namespace CatalogService.Application.Commands.FavoriteProducts.AddProductToFavorites;

public class AddProductToFavoritesResponse
{
    public Guid UserId { get; set; }
    public Guid ProductId { get; set; }
    public DateTime FavoritedAt { get; set; }
    public int TotalFavorites { get; set; }
}