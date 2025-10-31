using CatalogService.Domain.Entities;
using CatalogService.Domain.Repository;
using CatalogService.Infrastructure.Data.Context;
using Microsoft.Extensions.Logging;

namespace CatalogService.Infrastructure.Data.Repositories;

/// <summary>
/// Implementação do repositório para a entidade ReviewVote
/// </summary>
public class ReviewVoteRepository : Repository<ReviewVote>, IReviewVoteRepository
{
    public ReviewVoteRepository(CatalogDbContext context, ILogger<ReviewVoteRepository> logger) 
        : base(context, logger)
    {
    }
}