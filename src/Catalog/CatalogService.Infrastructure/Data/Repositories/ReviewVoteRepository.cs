using System.Linq.Expressions;
using CatalogService.Domain.Entities;
using CatalogService.Domain.Repository;
using CatalogService.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CatalogService.Infrastructure.Data.Repositories;

/// <summary>
/// Implementação do repositório para a entidade ReviewVote
/// </summary>
public class ReviewVoteRepository : IReviewVoteRepository
{
    private readonly CatalogDbContext _context;
    private readonly ILogger<ReviewVoteRepository> _logger;

    public ReviewVoteRepository(CatalogDbContext context, ILogger<ReviewVoteRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ReviewVote?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Obtendo voto de avaliação por ID: {VoteId}", id);
            
            var vote = await _context.ReviewVotes
                .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
            
            if (vote == null)
            {
                _logger.LogWarning("Voto de avaliação com ID {VoteId} não encontrado", id);
            }
            
            return vote;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter voto de avaliação por ID: {VoteId}", id);
            throw;
        }
    }

    public async Task<IReadOnlyList<ReviewVote>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Obtendo todos os votos de avaliação");
            
            var votes = await _context.ReviewVotes
                .OrderBy(v => v.CreatedAt)
                .ToListAsync(cancellationToken);
            
            _logger.LogDebug("Recuperados {Count} votos de avaliação", votes.Count);
            return votes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter todos os votos de avaliação");
            throw;
        }
    }

    public async Task<IReadOnlyList<ReviewVote>> FindAsync(Expression<Func<ReviewVote, bool>> predicate, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Buscando votos de avaliação com predicado");
            
            var votes = await _context.ReviewVotes
                .Where(predicate)
                .OrderBy(v => v.CreatedAt)
                .ToListAsync(cancellationToken);
            
            _logger.LogDebug("Encontrados {Count} votos de avaliação que correspondem ao predicado", votes.Count);
            return votes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar votos de avaliação com predicado");
            throw;
        }
    }

    public async Task<ReviewVote> AddAsync(ReviewVote entity, CancellationToken cancellationToken = default)
    {
        try
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _logger.LogDebug("Adicionando novo voto de avaliação para usuário: {UserId}, avaliação: {ReviewId}", entity.UserId, entity.ReviewId);
            
            var entry = await _context.ReviewVotes.AddAsync(entity, cancellationToken);
            
            _logger.LogInformation("Voto de avaliação adicionado com sucesso com ID: {VoteId} para usuário: {UserId}, avaliação: {ReviewId}", 
                entity.Id, entity.UserId, entity.ReviewId);
            
            return entry.Entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao adicionar voto de avaliação para usuário: {UserId}, avaliação: {ReviewId}", entity?.UserId, entity?.ReviewId);
            throw;
        }
    }

    public void Update(ReviewVote entity)
    {
        try
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _logger.LogDebug("Atualizando voto de avaliação: {VoteId}", entity.Id);
            
            _context.ReviewVotes.Update(entity);
            
            _logger.LogInformation("Voto de avaliação {VoteId} atualizado com sucesso", entity.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar voto de avaliação: {VoteId}", entity?.Id);
            throw;
        }
    }

    public void Remove(ReviewVote entity)
    {
        try
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _logger.LogDebug("Removendo voto de avaliação: {VoteId}", entity.Id);
            
            _context.ReviewVotes.Remove(entity);
            
            _logger.LogInformation("Voto de avaliação {VoteId} removido com sucesso", entity.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao remover voto de avaliação: {VoteId}", entity?.Id);
            throw;
        }
    }
}