using System.Linq.Expressions;
using CatalogService.Domain.Entities;
using CatalogService.Domain.Repository;
using CatalogService.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CatalogService.Infrastructure.Data.Repositories;

/// <summary>
/// Implementação do repositório para a entidade OutboxEvent
/// </summary>
public class OutboxEventRepository : IOutboxEventRepository
{
    private readonly CatalogDbContext _context;
    private readonly ILogger<OutboxEventRepository> _logger;

    public OutboxEventRepository(CatalogDbContext context, ILogger<OutboxEventRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<OutboxEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Obtendo evento de saída por ID: {EventId}", id);
            
            var outboxEvent = await _context.OutboxEvents
                .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
            
            if (outboxEvent == null)
            {
                _logger.LogWarning("Evento de saída com ID {EventId} não encontrado", id);
            }
            
            return outboxEvent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter evento de saída por ID: {EventId}", id);
            throw;
        }
    }

    public async Task<IReadOnlyList<OutboxEvent>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Obtendo todos os eventos de saída");
            
            var events = await _context.OutboxEvents
                .OrderBy(e => e.CreatedAt)
                .ToListAsync(cancellationToken);
            
            _logger.LogDebug("Recuperados {Count} eventos de saída", events.Count);
            return events;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter todos os eventos de saída");
            throw;
        }
    }

    public async Task<IReadOnlyList<OutboxEvent>> FindAsync(Expression<Func<OutboxEvent, bool>> predicate, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Buscando eventos de saída com predicado");
            
            var events = await _context.OutboxEvents
                .Where(predicate)
                .OrderBy(e => e.CreatedAt)
                .ToListAsync(cancellationToken);
            
            _logger.LogDebug("Encontrados {Count} eventos de saída que correspondem ao predicado", events.Count);
            return events;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar eventos de saída com predicado");
            throw;
        }
    }

    public async Task<OutboxEvent> AddAsync(OutboxEvent entity, CancellationToken cancellationToken = default)
    {
        try
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _logger.LogDebug("Adicionando novo evento de saída: {EventType}", entity.EventType);
            
            var entry = await _context.OutboxEvents.AddAsync(entity, cancellationToken);
            
            _logger.LogInformation("Evento de saída {EventType} adicionado com sucesso com ID: {EventId}", 
                entity.EventType, entity.Id);
            
            return entry.Entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao adicionar evento de saída: {EventType}", entity?.EventType);
            throw;
        }
    }

    public void Update(OutboxEvent entity)
    {
        try
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _logger.LogDebug("Atualizando evento de saída: {EventId}", entity.Id);
            
            _context.OutboxEvents.Update(entity);
            
            _logger.LogInformation("Evento de saída {EventId} atualizado com sucesso", entity.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar evento de saída: {EventId}", entity?.Id);
            throw;
        }
    }

    public void Remove(OutboxEvent entity)
    {
        try
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _logger.LogDebug("Removendo evento de saída: {EventId}", entity.Id);
            
            _context.OutboxEvents.Remove(entity);
            
            _logger.LogInformation("Evento de saída {EventId} removido com sucesso", entity.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao remover evento de saída: {EventId}", entity?.Id);
            throw;
        }
    }
}