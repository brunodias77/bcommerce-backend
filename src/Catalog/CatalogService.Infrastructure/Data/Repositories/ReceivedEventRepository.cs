using System.Linq.Expressions;
using CatalogService.Domain.Entities;
using CatalogService.Domain.Repository;
using CatalogService.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CatalogService.Infrastructure.Data.Repositories;

/// <summary>
/// Implementação do repositório para a entidade ReceivedEvent
/// </summary>
public class ReceivedEventRepository : IReceivedEventRepository
{
    private readonly CatalogDbContext _context;
    private readonly ILogger<ReceivedEventRepository> _logger;

    public ReceivedEventRepository(CatalogDbContext context, ILogger<ReceivedEventRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ReceivedEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Obtendo evento recebido por ID: {EventId}", id);
            
            var receivedEvent = await _context.ReceivedEvents
                .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
            
            if (receivedEvent == null)
            {
                _logger.LogWarning("Evento recebido com ID {EventId} não encontrado", id);
            }
            
            return receivedEvent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter evento recebido por ID: {EventId}", id);
            throw;
        }
    }

    public async Task<IReadOnlyList<ReceivedEvent>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Obtendo todos os eventos recebidos");
            
            var events = await _context.ReceivedEvents
                .OrderBy(e => e.CreatedAt)
                .ToListAsync(cancellationToken);
            
            _logger.LogDebug("Recuperados {Count} eventos recebidos", events.Count);
            return events;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter todos os eventos recebidos");
            throw;
        }
    }

    public async Task<IReadOnlyList<ReceivedEvent>> FindAsync(Expression<Func<ReceivedEvent, bool>> predicate, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Buscando eventos recebidos com predicado");
            
            var events = await _context.ReceivedEvents
                .Where(predicate)
                .OrderBy(e => e.CreatedAt)
                .ToListAsync(cancellationToken);
            
            _logger.LogDebug("Encontrados {Count} eventos recebidos que correspondem ao predicado", events.Count);
            return events;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar eventos recebidos com predicado");
            throw;
        }
    }

    public async Task<ReceivedEvent> AddAsync(ReceivedEvent entity, CancellationToken cancellationToken = default)
    {
        try
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _logger.LogDebug("Adicionando novo evento recebido: {EventType}", entity.EventType);
            
            var entry = await _context.ReceivedEvents.AddAsync(entity, cancellationToken);
            
            _logger.LogInformation("Evento recebido {EventType} adicionado com sucesso com ID: {EventId}", 
                entity.EventType, entity.Id);
            
            return entry.Entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao adicionar evento recebido: {EventType}", entity?.EventType);
            throw;
        }
    }

    public void Update(ReceivedEvent entity)
    {
        try
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _logger.LogDebug("Atualizando evento recebido: {EventId}", entity.Id);
            
            _context.ReceivedEvents.Update(entity);
            
            _logger.LogInformation("Evento recebido {EventId} atualizado com sucesso", entity.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar evento recebido: {EventId}", entity?.Id);
            throw;
        }
    }

    public void Remove(ReceivedEvent entity)
    {
        try
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _logger.LogDebug("Removendo evento recebido: {EventId}", entity.Id);
            
            _context.ReceivedEvents.Remove(entity);
            
            _logger.LogInformation("Evento recebido {EventId} removido com sucesso", entity.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao remover evento recebido: {EventId}", entity?.Id);
            throw;
        }
    }
}