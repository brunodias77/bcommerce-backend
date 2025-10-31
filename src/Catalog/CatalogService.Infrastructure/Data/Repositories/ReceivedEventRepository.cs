using CatalogService.Domain.Entities;
using CatalogService.Domain.Repository;
using CatalogService.Infrastructure.Data.Context;
using Microsoft.Extensions.Logging;

namespace CatalogService.Infrastructure.Data.Repositories;

/// <summary>
/// Implementação do repositório para a entidade ReceivedEvent
/// </summary>
public class ReceivedEventRepository : Repository<ReceivedEvent>, IReceivedEventRepository
{
    public ReceivedEventRepository(CatalogDbContext context, ILogger<ReceivedEventRepository> logger) 
        : base(context, logger)
    {
    }
}