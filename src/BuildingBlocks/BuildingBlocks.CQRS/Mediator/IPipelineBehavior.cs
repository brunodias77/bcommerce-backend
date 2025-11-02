namespace BuildingBlocks.CQRS.Mediator;

/// <summary>
/// Interface para behaviors que são executados no pipeline do Mediator
/// Permite interceptar e processar requests antes que cheguem aos handlers
/// </summary>
/// <typeparam name="TRequest">Tipo do request</typeparam>
/// <typeparam name="TResponse">Tipo da resposta</typeparam>
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Executa o behavior no pipeline
    /// </summary>
    /// <param name="request">Request a ser processado</param>
    /// <param name="next">Delegate para o próximo behavior ou handler</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Resposta do pipeline</returns>
    Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken = default);
}

/// <summary>
/// Delegate que representa o próximo behavior ou handler no pipeline
/// </summary>
/// <typeparam name="TResponse">Tipo da resposta</typeparam>
/// <returns>Resposta do próximo behavior ou handler</returns>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();