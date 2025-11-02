using BuildingBlocks.CQRS.Commands;
using BuildingBlocks.CQRS.Mediator;
using BuildingBlocks.CQRS.Validations;
using BuildingBlocks.Core.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.CQRS.Behaviors;

/// <summary>
/// Behavior responsável por executar validação automática de Commands
/// Intercepta requests no pipeline e executa validação antes do handler
/// </summary>
/// <typeparam name="TRequest">Tipo do Command</typeparam>
/// <typeparam name="TResponse">Tipo da resposta</typeparam>
public class ValidationBehavior<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
    where TResponse : class
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ValidationBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Construtor do ValidationBehavior
    /// </summary>
    /// <param name="serviceProvider">Service provider para resolver validators</param>
    /// <param name="logger">Logger para diagnóstico</param>
    public ValidationBehavior(IServiceProvider serviceProvider, ILogger<ValidationBehavior<TRequest, TResponse>> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executa a validação do request antes de prosseguir no pipeline
    /// </summary>
    /// <param name="request">Request a ser validado</param>
    /// <param name="next">Próximo behavior ou handler no pipeline</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Resposta do pipeline</returns>
    /// <exception cref="ValidationException">Lançada quando há erros de validação</exception>
    public async Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken = default)
    {
        var requestType = typeof(TRequest).Name;
        _logger.LogInformation("🔍 [ValidationBehavior] Iniciando validação para {RequestType}", requestType);
        
        // Buscar validator para o tipo de request
        var validator = _serviceProvider.GetService<IValidator<TRequest>>();
        
        if (validator != null)
        {
            _logger.LogInformation("✅ [ValidationBehavior] Validator encontrado para {RequestType}: {ValidatorType}", requestType, validator.GetType().Name);
            
            // Executar validação
            var validationResult = validator.Validate(request);
            
            // Se há erros, lançar ValidationException
            if (validationResult.HasErrors)
            {
                _logger.LogWarning("❌ [ValidationBehavior] Validação falhou para {RequestType}. Erros: {Errors}", 
                    requestType, string.Join("; ", validationResult.Errors.Select(e => e.Message)));
                throw new ValidationException(validationResult.Errors);
            }
            
            _logger.LogInformation("✅ [ValidationBehavior] Validação passou para {RequestType}", requestType);
        }
        else
        {
            _logger.LogInformation("⚠️ [ValidationBehavior] Nenhum validator encontrado para {RequestType}", requestType);
        }

        _logger.LogInformation("➡️ [ValidationBehavior] Continuando para próximo handler para {RequestType}", requestType);
        
        // Continuar para o próximo behavior ou handler
        return await next();
    }
}