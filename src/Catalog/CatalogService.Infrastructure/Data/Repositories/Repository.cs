using System.Linq.Expressions;
using BuildingBlocks.Core.Data;
using CatalogService.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CatalogService.Infrastructure.Data.Repositories;

/// <summary>
/// Implementação base genérica do padrão Repository
/// Fornece operações CRUD básicas usando Entity Framework Core
/// </summary>
/// <typeparam name="T">Tipo da entidade</typeparam>
public abstract class Repository<T> : IRepository<T> where T : class
{
    protected readonly CatalogDbContext Context;
    protected readonly DbSet<T> DbSet;
    protected readonly ILogger<Repository<T>> Logger;

    protected Repository(CatalogDbContext context, ILogger<Repository<T>> logger)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        DbSet = context.Set<T>();
    }

    /// <summary>
    /// Busca uma entidade por ID
    /// </summary>
    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            Logger.LogDebug("Buscando entidade {EntityType} com ID: {Id}", typeof(T).Name, id);
            
            var entity = await DbSet.FindAsync(new object[] { id }, cancellationToken);
            
            if (entity == null)
            {
                Logger.LogDebug("Entidade {EntityType} com ID {Id} não encontrada", typeof(T).Name, id);
            }
            else
            {
                Logger.LogDebug("Entidade {EntityType} com ID {Id} encontrada com sucesso", typeof(T).Name, id);
            }
            
            return entity;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao buscar entidade {EntityType} com ID: {Id}", typeof(T).Name, id);
            throw;
        }
    }

    /// <summary>
    /// Busca todas as entidades
    /// </summary>
    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            Logger.LogDebug("Buscando todas as entidades do tipo {EntityType}", typeof(T).Name);
            
            var entities = await DbSet.ToListAsync(cancellationToken);
            
            Logger.LogDebug("Encontradas {Count} entidades do tipo {EntityType}", entities.Count, typeof(T).Name);
            
            return entities.AsReadOnly();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao buscar todas as entidades do tipo {EntityType}", typeof(T).Name);
            throw;
        }
    }

    /// <summary>
    /// Busca entidades que atendem ao predicado especificado
    /// </summary>
    public virtual async Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        try
        {
            Logger.LogDebug("Buscando entidades {EntityType} com predicado personalizado", typeof(T).Name);
            
            var entities = await DbSet.Where(predicate).ToListAsync(cancellationToken);
            
            Logger.LogDebug("Encontradas {Count} entidades {EntityType} que atendem ao predicado", entities.Count, typeof(T).Name);
            
            return entities.AsReadOnly();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao buscar entidades {EntityType} com predicado", typeof(T).Name);
            throw;
        }
    }

    /// <summary>
    /// Adiciona uma nova entidade
    /// </summary>
    public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        try
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            Logger.LogDebug("Adicionando nova entidade do tipo {EntityType}", typeof(T).Name);
            
            var entityEntry = await DbSet.AddAsync(entity, cancellationToken);
            
            Logger.LogDebug("Entidade {EntityType} adicionada com sucesso ao contexto", typeof(T).Name);
            
            return entityEntry.Entity;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao adicionar entidade do tipo {EntityType}", typeof(T).Name);
            throw;
        }
    }

    /// <summary>
    /// Atualiza uma entidade existente
    /// </summary>
    public virtual void Update(T entity)
    {
        try
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            Logger.LogDebug("Atualizando entidade do tipo {EntityType}", typeof(T).Name);
            
            DbSet.Update(entity);
            
            Logger.LogDebug("Entidade {EntityType} marcada para atualização", typeof(T).Name);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao atualizar entidade do tipo {EntityType}", typeof(T).Name);
            throw;
        }
    }

    /// <summary>
    /// Remove uma entidade
    /// </summary>
    public virtual void Remove(T entity)
    {
        try
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            Logger.LogDebug("Removendo entidade do tipo {EntityType}", typeof(T).Name);
            
            DbSet.Remove(entity);
            
            Logger.LogDebug("Entidade {EntityType} marcada para remoção", typeof(T).Name);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Erro ao remover entidade do tipo {EntityType}", typeof(T).Name);
            throw;
        }
    }
}