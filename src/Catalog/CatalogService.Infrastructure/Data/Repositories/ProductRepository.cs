using System.Linq.Expressions;
using CatalogService.Domain.Aggregates;
using CatalogService.Domain.Repository;
using CatalogService.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CatalogService.Infrastructure.Data.Repositories;

/// <summary>
/// Implementação do repositório para o agregado Product
/// </summary>
public class ProductRepository : IProductRepository
{
    private readonly CatalogDbContext _context;
    private readonly ILogger<ProductRepository> _logger;

    public ProductRepository(CatalogDbContext context, ILogger<ProductRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Obtendo produto por ID: {ProductId}", id);
            
            var product = await _context.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
            
            if (product == null)
            {
                _logger.LogWarning("Produto com ID {ProductId} não encontrado", id);
            }
            
            return product;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter produto por ID: {ProductId}", id);
            throw;
        }
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Obtendo todos os produtos");
            
            var products = await _context.Products
                .Include(p => p.Images)
                .OrderBy(p => p.Name)
                .ToListAsync(cancellationToken);
            
            _logger.LogDebug("Recuperados {Count} produtos", products.Count);
            return products;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter todos os produtos");
            throw;
        }
    }

    public async Task<IReadOnlyList<Product>> FindAsync(Expression<Func<Product, bool>> predicate, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Buscando produtos com predicado");
            
            var products = await _context.Products
                .Include(p => p.Images)
                .Where(predicate)
                .OrderBy(p => p.Name)
                .ToListAsync(cancellationToken);
            
            _logger.LogDebug("Encontrados {Count} produtos que correspondem ao predicado", products.Count);
            return products;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar produtos com predicado");
            throw;
        }
    }

    public async Task<Product> AddAsync(Product entity, CancellationToken cancellationToken = default)
    {
        try
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _logger.LogDebug("Adicionando novo produto: {ProductName}", entity.Name);
            
            var entry = await _context.Products.AddAsync(entity, cancellationToken);
            
            _logger.LogInformation("Produto {ProductName} adicionado com sucesso com ID: {ProductId}", 
                entity.Name, entity.Id);
            
            return entry.Entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao adicionar produto: {ProductName}", entity?.Name);
            throw;
        }
    }

    public void Update(Product entity)
    {
        try
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _logger.LogDebug("Atualizando produto: {ProductId}", entity.Id);
            
            _context.Products.Update(entity);
            
            _logger.LogInformation("Produto {ProductId} atualizado com sucesso", entity.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar produto: {ProductId}", entity?.Id);
            throw;
        }
    }

    public void Remove(Product entity)
    {
        try
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _logger.LogDebug("Removendo produto: {ProductId}", entity.Id);
            
            _context.Products.Remove(entity);
            
            _logger.LogInformation("Produto {ProductId} removido com sucesso", entity.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao remover produto: {ProductId}", entity?.Id);
            throw;
        }
    }
}