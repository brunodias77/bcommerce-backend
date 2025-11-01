using System.Linq.Expressions;
using CatalogService.Domain.Entities;
using CatalogService.Domain.Repository;
using CatalogService.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CatalogService.Infrastructure.Data.Repositories;

/// <summary>
/// Implementação do repositório para a entidade FavoriteProduct
/// </summary>
public class FavoriteProductRepository : IFavoriteProductRepository
{
    private readonly CatalogDbContext _context;
    private readonly ILogger<FavoriteProductRepository> _logger;

    public FavoriteProductRepository(CatalogDbContext context, ILogger<FavoriteProductRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<FavoriteProduct?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Obtendo produto favorito por ID: {FavoriteId}", id);
            
            var favorite = await _context.FavoriteProducts
                .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
            
            if (favorite == null)
            {
                _logger.LogWarning("Produto favorito com ID {FavoriteId} não encontrado", id);
            }
            
            return favorite;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter produto favorito por ID: {FavoriteId}", id);
            throw;
        }
    }

    public async Task<IReadOnlyList<FavoriteProduct>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Obtendo todos os produtos favoritos");
            
            var favorites = await _context.FavoriteProducts
                .OrderBy(f => f.CreatedAt)
                .ToListAsync(cancellationToken);
            
            _logger.LogDebug("Recuperados {Count} produtos favoritos", favorites.Count);
            return favorites;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter todos os produtos favoritos");
            throw;
        }
    }

    public async Task<IReadOnlyList<FavoriteProduct>> FindAsync(Expression<Func<FavoriteProduct, bool>> predicate, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Buscando produtos favoritos com predicado");
            
            var favorites = await _context.FavoriteProducts
                .Where(predicate)
                .OrderBy(f => f.CreatedAt)
                .ToListAsync(cancellationToken);
            
            _logger.LogDebug("Encontrados {Count} produtos favoritos que correspondem ao predicado", favorites.Count);
            return favorites;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar produtos favoritos com predicado");
            throw;
        }
    }

    public async Task<FavoriteProduct> AddAsync(FavoriteProduct entity, CancellationToken cancellationToken = default)
    {
        try
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _logger.LogDebug("Adicionando novo produto favorito para usuário: {UserId}, produto: {ProductId}", entity.UserId, entity.ProductId);
            
            var entry = await _context.FavoriteProducts.AddAsync(entity, cancellationToken);
            
            _logger.LogInformation("Produto favorito adicionado com sucesso com ID: {FavoriteId} para usuário: {UserId}, produto: {ProductId}", 
                entity.Id, entity.UserId, entity.ProductId);
            
            return entry.Entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao adicionar produto favorito para usuário: {UserId}, produto: {ProductId}", entity?.UserId, entity?.ProductId);
            throw;
        }
    }

    public void Update(FavoriteProduct entity)
    {
        try
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _logger.LogDebug("Atualizando produto favorito: {FavoriteId}", entity.Id);
            
            _context.FavoriteProducts.Update(entity);
            
            _logger.LogInformation("Produto favorito {FavoriteId} atualizado com sucesso", entity.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar produto favorito: {FavoriteId}", entity?.Id);
            throw;
        }
    }

    public void Remove(FavoriteProduct entity)
    {
        try
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _logger.LogDebug("Removendo produto favorito: {FavoriteId}", entity.Id);
            
            _context.FavoriteProducts.Remove(entity);
            
            _logger.LogInformation("Produto favorito {FavoriteId} removido com sucesso", entity.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao remover produto favorito: {FavoriteId}", entity?.Id);
            throw;
        }
    }
}