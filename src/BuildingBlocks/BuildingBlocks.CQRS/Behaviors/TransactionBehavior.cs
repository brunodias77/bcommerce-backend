using BuildingBlocks.CQRS.Commands;
using BuildingBlocks.CQRS.Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Data;
using BuildingBlocks.Core.Data;

namespace BuildingBlocks.CQRS.Behaviors;

/// <summary>
/// Behavior responsável por gerenciar transações de banco de dados automaticamente usando IUnitOfWork.
/// Intercepta commands no pipeline e gerencia transações (commit/rollback).
/// Este behavior garante que todas as operações de escrita sejam executadas dentro de uma transação.
/// </summary>
/// <typeparam name="TRequest">Tipo do Command</typeparam>
/// <typeparam name="TResponse">Tipo da resposta</typeparam>
public class TransactionBehavior<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
    where TResponse : class
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Construtor do TransactionBehavior
    /// </summary>
    /// <param name="unitOfWork">Unit of Work para gerenciar transações</param>
    /// <param name="logger">Logger para diagnóstico</param>
    public TransactionBehavior(
        IUnitOfWork unitOfWork, 
        ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executa o behavior no pipeline, gerenciando transações automaticamente.
    /// 
    /// Fluxo de execução:
    /// 1. Inicia uma transação usando UnitOfWork
    /// 2. Executa o próximo behavior/handler no pipeline
    /// 3. Se sucesso: Faz commit da transação e persiste as mudanças
    /// 4. Se erro: Faz rollback da transação e propaga a exceção
    /// </summary>
    /// <param name="request">Command a ser processado</param>
    /// <param name="next">Delegate para o próximo behavior ou handler</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Resposta do pipeline</returns>
    public async Task<TResponse> HandleAsync(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken = default)
    {
        var requestType = typeof(TRequest).Name;
        
        _logger.LogInformation(
            "🔄 [TransactionBehavior] Iniciando transação para {RequestType}", 
            requestType
        );

        // Inicia a transação
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        
        _logger.LogDebug(
            "✅ [TransactionBehavior] Transação iniciada com sucesso para {RequestType}", 
            requestType
        );

        try
        {
            // Executa o próximo behavior/handler no pipeline
            _logger.LogDebug(
                "➡️ [TransactionBehavior] Executando handler para {RequestType}", 
                requestType
            );
            
            var result = await next();
            
            _logger.LogDebug(
                "✅ [TransactionBehavior] Handler executado com sucesso para {RequestType}", 
                requestType
            );
            
            // Se chegou até aqui, significa que o handler foi executado com sucesso
            // Agora vamos fazer commit da transação
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            
            _logger.LogInformation(
                "✅ [TransactionBehavior] Transação commitada com sucesso para {RequestType}", 
                requestType
            );
            
            return result;
        }
        catch (Exception ex)
        {
            // Em caso de qualquer erro, fazer rollback da transação
            _logger.LogError(
                ex,
                "❌ [TransactionBehavior] Erro durante execução de {RequestType}. Iniciando rollback...",
                requestType
            );

            try
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                
                _logger.LogWarning(
                    "🔄 [TransactionBehavior] Rollback executado com sucesso para {RequestType}", 
                    requestType
                );
            }
            catch (Exception rollbackEx)
            {
                // Se o rollback também falhar, logar o erro mas manter a exceção original
                _logger.LogCritical(
                    rollbackEx,
                    "💥 [TransactionBehavior] ERRO CRÍTICO: Falha ao executar rollback para {RequestType}. " +
                    "Exceção do Rollback: {RollbackExceptionType}, Mensagem: {RollbackExceptionMessage}",
                    requestType,
                    rollbackEx.GetType().Name,
                    rollbackEx.Message
                );
                
                // Propaga a exceção original, não a do rollback
                throw;
            }
            
            // Propaga a exceção original após o rollback
            throw;
        }
    }
}