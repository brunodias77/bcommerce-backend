using System.Linq.Expressions;
using CatalogService.Domain.Entities;
using CatalogService.Domain.Repository;
using CatalogService.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CatalogService.Infrastructure.Data.Repositories;

/// <summary>
/// Implementação do repositório para a entidade InboxEvent
/// </summary>
public class InboxEventRepository : IInboxEventRepository
{
    private readonly CatalogDbContext _context;
    private readonly ILogger<InboxEventRepository> _logger;

    public InboxEventRepository(CatalogDbContext context, ILogger<InboxEventRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<InboxEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Obtendo evento de entrada por ID: {EventId}", id);
            
            var inboxEvent = await _context.InboxEvents
                .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
            
            if (inboxEvent == null)
            {
                _logger.LogWarning("Evento de entrada com ID {EventId} não encontrado", id);
            }
            
            return inboxEvent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter evento de entrada por ID: {EventId}", id);
            throw;
        }
    }

    public async Task<IReadOnlyList<InboxEvent>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Obtendo todos os eventos de entrada");
            
            var events = await _context.InboxEvents
                .OrderBy(e => e.CreatedAt)
                .ToListAsync(cancellationToken);
            
            _logger.LogDebug("Recuperados {Count} eventos de entrada", events.Count);
            return events;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter todos os eventos de entrada");
            throw;
        }
    }

    public async Task<IReadOnlyList<InboxEvent>> FindAsync(Expression<Func<InboxEvent, bool>> predicate, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Buscando eventos de entrada com predicado");
            
            var events = await _context.InboxEvents
                .Where(predicate)
                .OrderBy(e => e.CreatedAt)
                .ToListAsync(cancellationToken);
            
            _logger.LogDebug("Encontrados {Count} eventos de entrada que correspondem ao predicado", events.Count);
            return events;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar eventos de entrada com predicado");
            throw;
        }
    }

    public async Task<InboxEvent> AddAsync(InboxEvent entity, CancellationToken cancellationToken = default)
    {
        try
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _logger.LogDebug("Adicionando novo evento de entrada: {EventType}", entity.EventType);
            
            var entry = await _context.InboxEvents.AddAsync(entity, cancellationToken);
            
            _logger.LogInformation("Evento de entrada {EventType} adicionado com sucesso com ID: {EventId}", 
                entity.EventType, entity.Id);
            
            return entry.Entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao adicionar evento de entrada: {EventType}", entity?.EventType);
            throw;
        }
    }

    public void Update(InboxEvent entity)
    {
        try
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _logger.LogDebug("Atualizando evento de entrada: {EventId}", entity.Id);
            
            _context.InboxEvents.Update(entity);
            
            _logger.LogInformation("Evento de entrada {EventId} atualizado com sucesso", entity.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar evento de entrada: {EventId}", entity?.Id);
            throw;
        }
    }

    public void Remove(InboxEvent entity)
    {
        try
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _logger.LogDebug("Removendo evento de entrada: {EventId}", entity.Id);
            
            _context.InboxEvents.Remove(entity);
            
            _logger.LogInformation("Evento de entrada {EventId} removido com sucesso", entity.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao remover evento de entrada: {EventId}", entity?.Id);
            throw;
        }
    }
}