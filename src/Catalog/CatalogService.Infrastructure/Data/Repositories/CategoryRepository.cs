using System.Linq.Expressions;
using CatalogService.Domain.Aggregates;
using CatalogService.Domain.Repository;
using CatalogService.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CatalogService.Infrastructure.Data.Repositories;

/// <summary>
/// Implementação do repositório para o agregado Category
/// </summary>
public class CategoryRepository : ICategoryRepository
{
    private readonly CatalogDbContext _context;
    private readonly ILogger<CategoryRepository> _logger;

    public CategoryRepository(CatalogDbContext context, ILogger<CategoryRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Obtendo categoria por ID: {CategoryId}", id);
            
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
            
            if (category == null)
            {
                _logger.LogWarning("Categoria com ID {CategoryId} não encontrada", id);
            }
            
            return category;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter categoria por ID: {CategoryId}", id);
            throw;
        }
    }

    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Obtendo todas as categorias");
            
            var categories = await _context.Categories
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync(cancellationToken);
            
            _logger.LogDebug("Recuperadas {Count} categorias", categories.Count);
            return categories;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter todas as categorias");
            throw;
        }
    }

    public async Task<IReadOnlyList<Category>> FindAsync(Expression<Func<Category, bool>> predicate, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Buscando categorias com predicado");
            
            var categories = await _context.Categories
                .Where(predicate)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync(cancellationToken);
            
            _logger.LogDebug("Encontradas {Count} categorias que correspondem ao predicado", categories.Count);
            return categories;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar categorias com predicado");
            throw;
        }
    }

    public async Task<Category> AddAsync(Category entity, CancellationToken cancellationToken = default)
    {
        try
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _logger.LogDebug("Adicionando nova categoria: {CategoryName}", entity.Name);
            
            var entry = await _context.Categories.AddAsync(entity, cancellationToken);
            
            _logger.LogInformation("Categoria {CategoryName} adicionada com sucesso com ID: {CategoryId}", 
                entity.Name, entity.Id);
            
            return entry.Entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao adicionar categoria: {CategoryName}", entity?.Name);
            throw;
        }
    }

    public void Update(Category entity)
    {
        try
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _logger.LogDebug("Atualizando categoria: {CategoryId}", entity.Id);
            
            _context.Categories.Update(entity);
            
            _logger.LogInformation("Categoria {CategoryId} atualizada com sucesso", entity.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar categoria: {CategoryId}", entity?.Id);
            throw;
        }
    }

    public void Remove(Category entity)
    {
        try
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _logger.LogDebug("Removendo categoria: {CategoryId}", entity.Id);
            
            _context.Categories.Remove(entity);
            
            _logger.LogInformation("Categoria {CategoryId} removida com sucesso", entity.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao remover categoria: {CategoryId}", entity?.Id);
            throw;
        }
    }
}