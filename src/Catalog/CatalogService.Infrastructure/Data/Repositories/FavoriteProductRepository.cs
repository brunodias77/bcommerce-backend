using CatalogService.Domain.Entities;
using CatalogService.Domain.Repository;
using CatalogService.Infrastructure.Data.Context;
using Microsoft.Extensions.Logging;

namespace CatalogService.Infrastructure.Data.Repositories;

/// <summary>
/// Implementação do repositório para a entidade FavoriteProduct
/// </summary>
public class FavoriteProductRepository : Repository<FavoriteProduct>, IFavoriteProductRepository
{
    public FavoriteProductRepository(CatalogDbContext context, ILogger<FavoriteProductRepository> logger) 
        : base(context, logger)
    {
    }
}