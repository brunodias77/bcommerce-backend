using BuildingBlocks.CQRS.Mediator;

namespace BuildingBlocks.CQRS.Queries;

/// <summary>
/// Interface para handlers de Queries
/// Herda de IRequestHandler para compatibilidade com o Mediator
/// </summary>
/// <typeparam name="TQuery">Tipo da Query</typeparam>
/// <typeparam name="TResponse">Tipo do resultado da Query</typeparam>
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
}