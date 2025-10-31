using CatalogService.Domain.Entities;
using CatalogService.Domain.Repository;
using CatalogService.Infrastructure.Data.Context;
using Microsoft.Extensions.Logging;

namespace CatalogService.Infrastructure.Data.Repositories;

/// <summary>
/// Implementação do repositório para a entidade ProductImage
/// </summary>
public class ProductImageRepository : Repository<ProductImage>, IProductImageRepository
{
    public ProductImageRepository(CatalogDbContext context, ILogger<ProductImageRepository> logger) 
        : base(context, logger)
    {
    }
}