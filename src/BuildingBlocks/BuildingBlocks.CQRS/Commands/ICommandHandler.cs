using BuildingBlocks.CQRS.Mediator;

namespace BuildingBlocks.CQRS.Commands;


/// <summary>
/// Interface para handlers de Commands (sem retorno)
/// Herda de IRequestHandler para compatibilidade com o Mediator
/// </summary>
/// <typeparam name="TCommand">Tipo do Command</typeparam>
public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand>
    where TCommand : ICommand
{
}

/// <summary>
/// Interface para handlers de Commands com retorno
/// Herda de IRequestHandler para compatibilidade com o Mediator
/// </summary>
/// <typeparam name="TCommand">Tipo do Command</typeparam>
/// <typeparam name="TResponse">Tipo do retorno</typeparam>
public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
}