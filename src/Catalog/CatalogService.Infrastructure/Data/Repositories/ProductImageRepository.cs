using System.Linq.Expressions;
using CatalogService.Domain.Entities;
using CatalogService.Domain.Repository;
using CatalogService.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CatalogService.Infrastructure.Data.Repositories;

/// <summary>
/// Implementação do repositório para a entidade ProductImage
/// </summary>
public class ProductImageRepository : IProductImageRepository
{
    private readonly CatalogDbContext _context;
    private readonly ILogger<ProductImageRepository> _logger;

    public ProductImageRepository(CatalogDbContext context, ILogger<ProductImageRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ProductImage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Obtendo imagem de produto por ID: {ImageId}", id);
            
            var image = await _context.ProductImages
                .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
            
            if (image == null)
            {
                _logger.LogWarning("Imagem de produto com ID {ImageId} não encontrada", id);
            }
            
            return image;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter imagem de produto por ID: {ImageId}", id);
            throw;
        }
    }

    public async Task<IReadOnlyList<ProductImage>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Obtendo todas as imagens de produtos");
            
            var images = await _context.ProductImages
                .OrderBy(i => i.DisplayOrder)
                .ThenBy(i => i.CreatedAt)
                .ToListAsync(cancellationToken);
            
            _logger.LogDebug("Recuperadas {Count} imagens de produtos", images.Count);
            return images;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter todas as imagens de produtos");
            throw;
        }
    }

    public async Task<IReadOnlyList<ProductImage>> FindAsync(Expression<Func<ProductImage, bool>> predicate, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Buscando imagens de produtos com predicado");
            
            var images = await _context.ProductImages
                .Where(predicate)
                .OrderBy(i => i.DisplayOrder)
                .ThenBy(i => i.CreatedAt)
                .ToListAsync(cancellationToken);
            
            _logger.LogDebug("Encontradas {Count} imagens de produtos que correspondem ao predicado", images.Count);
            return images;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar imagens de produtos com predicado");
            throw;
        }
    }

    public async Task<ProductImage> AddAsync(ProductImage entity, CancellationToken cancellationToken = default)
    {
        try
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _logger.LogDebug("Adicionando nova imagem de produto para produto: {ProductId}", entity.ProductId);
            
            var entry = await _context.ProductImages.AddAsync(entity, cancellationToken);
            
            _logger.LogInformation("Imagem de produto adicionada com sucesso com ID: {ImageId} para produto: {ProductId}", 
                entity.Id, entity.ProductId);
            
            return entry.Entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao adicionar imagem de produto para produto: {ProductId}", entity?.ProductId);
            throw;
        }
    }

    public void Update(ProductImage entity)
    {
        try
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _logger.LogDebug("Atualizando imagem de produto: {ImageId}", entity.Id);
            
            _context.ProductImages.Update(entity);
            
            _logger.LogInformation("Imagem de produto {ImageId} atualizada com sucesso", entity.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar imagem de produto: {ImageId}", entity?.Id);
            throw;
        }
    }

    public void Remove(ProductImage entity)
    {
        try
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _logger.LogDebug("Removendo imagem de produto: {ImageId}", entity.Id);
            
            _context.ProductImages.Remove(entity);
            
            _logger.LogInformation("Imagem de produto {ImageId} removida com sucesso", entity.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao remover imagem de produto: {ImageId}", entity?.Id);
            throw;
        }
    }
}