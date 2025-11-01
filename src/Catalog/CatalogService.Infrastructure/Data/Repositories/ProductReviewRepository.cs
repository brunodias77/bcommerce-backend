using System.Linq.Expressions;
using CatalogService.Domain.Aggregates;
using CatalogService.Domain.Repository;
using CatalogService.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CatalogService.Infrastructure.Data.Repositories;

/// <summary>
/// Implementação do repositório para o agregado ProductReview
/// </summary>
public class ProductReviewRepository : IProductReviewRepository
{
    private readonly CatalogDbContext _context;
    private readonly ILogger<ProductReviewRepository> _logger;

    public ProductReviewRepository(CatalogDbContext context, ILogger<ProductReviewRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ProductReview?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Obtendo avaliação de produto por ID: {ReviewId}", id);
            
            var review = await _context.ProductReviews
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
            
            if (review == null)
            {
                _logger.LogWarning("Avaliação de produto com ID {ReviewId} não encontrada", id);
            }
            
            return review;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter avaliação de produto por ID: {ReviewId}", id);
            throw;
        }
    }

    public async Task<IReadOnlyList<ProductReview>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Obtendo todas as avaliações de produtos");
            
            var reviews = await _context.ProductReviews
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync(cancellationToken);
            
            _logger.LogDebug("Recuperadas {Count} avaliações de produtos", reviews.Count);
            return reviews;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter todas as avaliações de produtos");
            throw;
        }
    }

    public async Task<IReadOnlyList<ProductReview>> FindAsync(Expression<Func<ProductReview, bool>> predicate, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Buscando avaliações de produtos com predicado");
            
            var reviews = await _context.ProductReviews
                .Where(predicate)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync(cancellationToken);
            
            _logger.LogDebug("Encontradas {Count} avaliações de produtos que correspondem ao predicado", reviews.Count);
            return reviews;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar avaliações de produtos com predicado");
            throw;
        }
    }

    public async Task<ProductReview> AddAsync(ProductReview entity, CancellationToken cancellationToken = default)
    {
        try
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _logger.LogDebug("Adicionando nova avaliação de produto para produto: {ProductId}", entity.ProductId);
            
            var entry = await _context.ProductReviews.AddAsync(entity, cancellationToken);
            
            _logger.LogInformation("Avaliação de produto adicionada com sucesso com ID: {ReviewId} para produto: {ProductId}", 
                entity.Id, entity.ProductId);
            
            return entry.Entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao adicionar avaliação de produto para produto: {ProductId}", entity?.ProductId);
            throw;
        }
    }

    public void Update(ProductReview entity)
    {
        try
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _logger.LogDebug("Atualizando avaliação de produto: {ReviewId}", entity.Id);
            
            _context.ProductReviews.Update(entity);
            
            _logger.LogInformation("Avaliação de produto {ReviewId} atualizada com sucesso", entity.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar avaliação de produto: {ReviewId}", entity?.Id);
            throw;
        }
    }

    public void Remove(ProductReview entity)
    {
        try
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _logger.LogDebug("Removendo avaliação de produto: {ReviewId}", entity.Id);
            
            _context.ProductReviews.Remove(entity);
            
            _logger.LogInformation("Avaliação de produto {ReviewId} removida com sucesso", entity.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao remover avaliação de produto: {ReviewId}", entity?.Id);
            throw;
        }
    }
}