using BuildingBlocks.CQRS.Mediator;

namespace BuildingBlocks.CQRS.Commands;

/// <summary>
/// Interface base para Commands (sem retorno)
/// Herda de IRequest para compatibilidade com o Mediator
/// </summary>
public interface ICommand : IRequest
{
}

/// <summary>
/// Interface base para Commands com retorno
/// Herda de IRequest<TResponse> para compatibilidade com o Mediator
/// </summary>
/// <typeparam name="TResponse">Tipo do retorno esperado</typeparam>
public interface ICommand<out TResponse> : IRequest<TResponse>
{
}