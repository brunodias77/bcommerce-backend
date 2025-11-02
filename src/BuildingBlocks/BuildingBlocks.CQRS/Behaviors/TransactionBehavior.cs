using BuildingBlocks.CQRS.Commands;
using BuildingBlocks.CQRS.Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Data;

namespace BuildingBlocks.CQRS.Behaviors;

/// <summary>
/// Behavior responsável por gerenciar transações de banco de dados automaticamente
/// Intercepta commands no pipeline e gerencia transações (commit/rollback)
/// </summary>
/// <typeparam name="TRequest">Tipo do Command</typeparam>
/// <typeparam name="TResponse">Tipo da resposta</typeparam>
public class TransactionBehavior<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
    where TResponse : class
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Construtor do TransactionBehavior
    /// </summary>
    /// <param name="serviceProvider">Service provider para resolver dependências</param>
    /// <param name="logger">Logger para diagnóstico</param>
    public TransactionBehavior(
        IServiceProvider serviceProvider, 
        ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executa o behavior no pipeline, gerenciando transações automaticamente
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
        _logger.LogInformation("🔄 TransactionBehavior iniciando transação para {RequestType}", requestType);

        // Buscar conexão de banco de dados via DI
        using var scope = _serviceProvider.CreateScope();
        var connection = scope.ServiceProvider.GetService<IDbConnection>();
        
        if (connection == null)
        {
            _logger.LogWarning("⚠️ Nenhuma conexão de banco encontrada para {RequestType}, executando sem transação", requestType);
            return await next();
        }

        // Garantir que a conexão está aberta
        if (connection.State != ConnectionState.Open)
        {
            connection.Open();
        }

        using var transaction = connection.BeginTransaction();
        _logger.LogInformation("✅ Transação iniciada para {RequestType}", requestType);

        try
        {
            // Executar o próximo behavior/handler no pipeline
            var result = await next();
            
            // Se chegou até aqui, fazer commit da transação
            transaction.Commit();
            _logger.LogInformation("✅ Transação commitada com sucesso para {RequestType}", requestType);
            
            return result;
        }
        catch (Exception ex)
        {
            // Em caso de erro, fazer rollback
            transaction.Rollback();
            _logger.LogError(ex, "❌ Erro durante execução de {RequestType}, transação revertida", requestType);
            throw;
        }
    }
}