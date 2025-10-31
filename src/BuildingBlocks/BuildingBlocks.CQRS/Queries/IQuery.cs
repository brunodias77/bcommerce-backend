using BuildingBlocks.CQRS.Mediator;

namespace BuildingBlocks.CQRS.Queries;

/// <summary>
/// Interface base para Queries
/// Herda de IRequest<TResponse> para compatibilidade com o Mediator
/// Queries sempre retornam um resultado
/// </summary>
/// <typeparam name="TResponse">Tipo do resultado esperado da Query</typeparam>
public interface IQuery<out TResponse> : IRequest<TResponse>
{
}