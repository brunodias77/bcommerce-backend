using BuildingBlocks.Core.Data;
using CatalogService.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace CatalogService.Infrastructure.Data.UnitOfWork;

/// <summary>
/// Implementação do padrão Unit of Work para o CatalogService.
/// Gerencia transações e operações de persistência de dados usando Entity Framework Core.
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
    /// Indica se existe uma transação ativa no momento
    /// </summary>
    public bool HasActiveTransaction => _currentTransaction != null;

    /// <summary>
    /// Retorna o ID da transação atual (se houver)
    /// </summary>
    public Guid? CurrentTransactionId => _currentTransaction?.TransactionId;

    /// <summary>
    /// Salva todas as mudanças pendentes no contexto.
    /// 
    /// IMPORTANTE: Este método NÃO faz commit da transação automaticamente.
    /// O commit deve ser feito explicitamente via CommitTransactionAsync.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Número de entidades afetadas</returns>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug(
                "[UnitOfWork] Iniciando SaveChangesAsync. Transação ativa: {HasTransaction}",
                HasActiveTransaction
            );
            
            var result = await _context.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation(
                "✅ [UnitOfWork] SaveChangesAsync concluído com sucesso. " +
                "{EntitiesAffected} entidade(s) afetada(s). Transação ativa: {HasTransaction}",
                result,
                HasActiveTransaction
            );
            
            return result;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(
                ex,
                "❌ [UnitOfWork] Erro de concorrência detectado durante SaveChangesAsync. " +
                "Entidade: {EntityName}",
                ex.Entries.FirstOrDefault()?.Entity.GetType().Name ?? "Desconhecida"
            );
            throw;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(
                ex,
                "❌ [UnitOfWork] Erro de atualização do banco de dados durante SaveChangesAsync. " +
                "Inner Exception: {InnerException}",
                ex.InnerException?.Message ?? "Nenhuma"
            );
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "❌ [UnitOfWork] Erro inesperado durante SaveChangesAsync. " +
                "Tipo: {ExceptionType}",
                ex.GetType().Name
            );
            throw;
        }
    }

    /// <summary>
    /// Inicia uma nova transação de banco de dados.
    /// 
    /// IMPORTANTE: Apenas uma transação pode estar ativa por vez.
    /// Se já existe uma transação ativa, lança InvalidOperationException.
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <exception cref="InvalidOperationException">Se já existe uma transação ativa</exception>
    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null)
        {
            var errorMessage = $"Tentativa de iniciar nova transação quando já existe uma ativa. " +
                              $"TransactionId atual: {_currentTransaction.TransactionId}";
            
            _logger.LogError("[UnitOfWork] {ErrorMessage}", errorMessage);
            throw new InvalidOperationException(errorMessage);
        }

        try
        {
            _logger.LogDebug("[UnitOfWork] Iniciando nova transação...");
            
            _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            
            _logger.LogInformation(
                "✅ [UnitOfWork] Transação iniciada com sucesso. TransactionId: {TransactionId}",
                _currentTransaction.TransactionId
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "❌ [UnitOfWork] Erro ao iniciar transação. Tipo: {ExceptionType}, " +
                "Mensagem: {ExceptionMessage}",
                ex.GetType().Name,
                ex.Message
            );
            throw;
        }
    }

    /// <summary>
    /// Confirma a transação atual, persistindo todas as mudanças no banco de dados.
    /// 
    /// Este método:
    /// 1. Valida se existe uma transação ativa
    /// 2. Faz commit da transação
    /// 3. Libera os recursos da transação
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <exception cref="InvalidOperationException">Se não existe transação ativa</exception>
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null)
        {
            var errorMessage = "Tentativa de commit sem transação ativa";
            _logger.LogError("[UnitOfWork] {ErrorMessage}", errorMessage);
            throw new InvalidOperationException(errorMessage);
        }

        var transactionId = _currentTransaction.TransactionId;

        try
        {
            _logger.LogDebug(
                "[UnitOfWork] Iniciando commit da transação. TransactionId: {TransactionId}",
                transactionId
            );
            
            // Commit da transação
            await _currentTransaction.CommitAsync(cancellationToken);
            
            _logger.LogInformation(
                "✅ [UnitOfWork] Transação confirmada com sucesso. TransactionId: {TransactionId}",
                transactionId
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "❌ [UnitOfWork] Erro ao confirmar transação. TransactionId: {TransactionId}, " +
                "Tipo: {ExceptionType}, Mensagem: {ExceptionMessage}",
                transactionId,
                ex.GetType().Name,
                ex.Message
            );
            
            // Tenta fazer rollback em caso de erro no commit
            try
            {
                await RollbackTransactionAsync(cancellationToken);
            }
            catch (Exception rollbackEx)
            {
                _logger.LogCritical(
                    rollbackEx,
                    "💥 [UnitOfWork] ERRO CRÍTICO: Falha no rollback após erro no commit. " +
                    "TransactionId: {TransactionId}",
                    transactionId
                );
            }
            
            throw;
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    /// <summary>
    /// Desfaz a transação atual, revertendo todas as mudanças.
    /// 
    /// Este método:
    /// 1. Valida se existe uma transação ativa
    /// 2. Faz rollback da transação
    /// 3. Libera os recursos da transação
    /// </summary>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <exception cref="InvalidOperationException">Se não existe transação ativa</exception>
    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null)
        {
            var errorMessage = "Tentativa de rollback sem transação ativa";
            _logger.LogWarning("[UnitOfWork] {ErrorMessage}", errorMessage);
            throw new InvalidOperationException(errorMessage);
        }

        var transactionId = _currentTransaction.TransactionId;

        try
        {
            _logger.LogDebug(
                "[UnitOfWork] Iniciando rollback da transação. TransactionId: {TransactionId}",
                transactionId
            );
            
            await _currentTransaction.RollbackAsync(cancellationToken);
            
            _logger.LogInformation(
                "🔄 [UnitOfWork] Transação desfeita com sucesso. TransactionId: {TransactionId}",
                transactionId
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "❌ [UnitOfWork] Erro ao desfazer transação. TransactionId: {TransactionId}, " +
                "Tipo: {ExceptionType}, Mensagem: {ExceptionMessage}",
                transactionId,
                ex.GetType().Name,
                ex.Message
            );
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
                    _logger.LogWarning(
                        "⚠️ [UnitOfWork] Descartando UnitOfWork com transação ativa. " +
                        "TransactionId: {TransactionId}. Fazendo rollback automático...",
                        _currentTransaction.TransactionId
                    );
                    
                    _currentTransaction.Rollback();
                    _currentTransaction.Dispose();
                    _currentTransaction = null;
                }

                _logger.LogDebug("[UnitOfWork] UnitOfWork descartado com sucesso");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "❌ [UnitOfWork] Erro ao descartar UnitOfWork. Tipo: {ExceptionType}",
                    ex.GetType().Name
                );
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
            _logger.LogDebug(
                "[UnitOfWork] Liberando recursos da transação. TransactionId: {TransactionId}",
                _currentTransaction.TransactionId
            );
            
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
            
            _logger.LogDebug("[UnitOfWork] Recursos da transação liberados");
        }
    }
}