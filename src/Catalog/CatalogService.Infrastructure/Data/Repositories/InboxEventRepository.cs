using CatalogService.Domain.Entities;
using CatalogService.Domain.Repository;
using CatalogService.Infrastructure.Data.Context;
using Microsoft.Extensions.Logging;

namespace CatalogService.Infrastructure.Data.Repositories;

/// <summary>
/// Implementação do repositório para a entidade InboxEvent
/// </summary>
public class InboxEventRepository : Repository<InboxEvent>, IInboxEventRepository
{
    public InboxEventRepository(CatalogDbContext context, ILogger<InboxEventRepository> logger) 
        : base(context, logger)
    {
    }
}