using BuildingBlocks.Core.Data;
using CatalogService.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace CatalogService.Infrastructure.Data.UnitOfWork;

/// <summary>
/// Implementação do padrão Unit of Work para o CatalogService
/// Gerencia transações e operações de persistência de dados
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly CatalogDbContext _context;
    private readonly ILogger<UnitOfWork> _logger;
    private IDbContextTransaction? _currentTransaction;
    private bool _disposed = false;

    public UnitOfWork(CatalogDbContext context, ILogger<UnitOfWork> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Salva todas as mudanças pendentes no contexto
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Número de entidades afetadas</returns>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Iniciando SaveChangesAsync no UnitOfWork");
            
            var result = await _context.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("SaveChangesAsync concluído com sucesso. {EntitiesAffected} entidades afetadas", result);
            
            return result;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Erro de concorrência detectado durante SaveChangesAsync");
            throw;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Erro de atualização do banco de dados durante SaveChangesAsync");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado durante SaveChangesAsync");
            throw;
        }
    }

    /// <summary>
    /// Inicia uma nova transação de banco de dados
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento</param>
    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null)
        {
            _logger.LogWarning("Tentativa de iniciar uma nova transação quando já existe uma ativa");
            throw new InvalidOperationException("Uma transação já está ativa. Finalize a transação atual antes de iniciar uma nova.");
        }

        try
        {
            _logger.LogDebug("Iniciando nova transação");
            
            _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            
            _logger.LogInformation("Transação iniciada com sucesso. TransactionId: {TransactionId}", 
                _currentTransaction.TransactionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao iniciar transação");
            throw;
        }
    }

    /// <summary>
    /// Confirma a transação atual
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento</param>
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null)
        {
            _logger.LogWarning("Tentativa de commit sem transação ativa");
            throw new InvalidOperationException("Nenhuma transação ativa para confirmar.");
        }

        try
        {
            var transactionId = _currentTransaction.TransactionId;
            _logger.LogDebug("Confirmando transação. TransactionId: {TransactionId}", transactionId);
            
            await _currentTransaction.CommitAsync(cancellationToken);
            
            _logger.LogInformation("Transação confirmada com sucesso. TransactionId: {TransactionId}", transactionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao confirmar transação. TransactionId: {TransactionId}", 
                _currentTransaction.TransactionId);
            throw;
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    /// <summary>
    /// Desfaz a transação atual
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento</param>
    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null)
        {
            _logger.LogWarning("Tentativa de rollback sem transação ativa");
            throw new InvalidOperationException("Nenhuma transação ativa para desfazer.");
        }

        try
        {
            var transactionId = _currentTransaction.TransactionId;
            _logger.LogDebug("Desfazendo transação. TransactionId: {TransactionId}", transactionId);
            
            await _currentTransaction.RollbackAsync(cancellationToken);
            
            _logger.LogInformation("Transação desfeita com sucesso. TransactionId: {TransactionId}", transactionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao desfazer transação. TransactionId: {TransactionId}", 
                _currentTransaction.TransactionId);
            throw;
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    /// <summary>
    /// Libera os recursos utilizados pelo UnitOfWork
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Implementação do padrão Dispose
    /// </summary>
    /// <param name="disposing">Indica se está sendo chamado pelo Dispose ou pelo finalizer</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            try
            {
                if (_currentTransaction != null)
                {
                    _logger.LogWarning("Descartando UnitOfWork com transação ativa. Fazendo rollback automático");
                    _currentTransaction.Rollback();
                    _currentTransaction.Dispose();
                    _currentTransaction = null;
                }

                _logger.LogDebug("UnitOfWork descartado com sucesso");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao descartar UnitOfWork");
            }
            finally
            {
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Descarta a transação atual de forma assíncrona
    /// </summary>
    private async Task DisposeTransactionAsync()
    {
        if (_currentTransaction != null)
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }
}