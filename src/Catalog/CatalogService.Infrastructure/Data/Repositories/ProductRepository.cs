using CatalogService.Domain.Aggregates;
using CatalogService.Domain.Repository;
using CatalogService.Infrastructure.Data.Context;
using Microsoft.Extensions.Logging;

namespace CatalogService.Infrastructure.Data.Repositories;

/// <summary>
/// Implementação do repositório para o agregado Product
/// </summary>
public class ProductRepository : Repository<Product>, IProductRepository
{
    public ProductRepository(CatalogDbContext context, ILogger<ProductRepository> logger) 
        : base(context, logger)
    {
    }
}