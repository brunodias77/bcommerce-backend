using CatalogService.Domain.Aggregates;
using CatalogService.Domain.Repository;
using CatalogService.Infrastructure.Data.Context;
using Microsoft.Extensions.Logging;

namespace CatalogService.Infrastructure.Data.Repositories;

/// <summary>
/// Implementação do repositório para o agregado ProductReview
/// </summary>
public class ProductReviewRepository : Repository<ProductReview>, IProductReviewRepository
{
    public ProductReviewRepository(CatalogDbContext context, ILogger<ProductReviewRepository> logger) 
        : base(context, logger)
    {
    }
}