using BuildingBlocks.CQRS.Mediator;
using BuildingBlocks.Core.Events;

namespace BuildingBlocks.CQRS.Events;

/// <summary>
/// Interface para Domain Events no contexto CQRS
/// Herda de IDomainEvent do Core e INotification para compatibilidade com o Mediator
/// </summary>
public interface IDomainEvent : Core.Events.IDomainEvent, INotification
{
}