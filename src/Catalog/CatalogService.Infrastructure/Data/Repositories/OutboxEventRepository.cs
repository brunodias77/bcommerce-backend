using CatalogService.Domain.Entities;
using CatalogService.Domain.Repository;
using CatalogService.Infrastructure.Data.Context;
using Microsoft.Extensions.Logging;

namespace CatalogService.Infrastructure.Data.Repositories;

/// <summary>
/// Implementação do repositório para a entidade OutboxEvent
/// </summary>
public class OutboxEventRepository : Repository<OutboxEvent>, IOutboxEventRepository
{
    public OutboxEventRepository(CatalogDbContext context, ILogger<OutboxEventRepository> logger) 
        : base(context, logger)
    {
    }
}