using BuildingBlocks.CQRS.Mediator;

namespace BuildingBlocks.CQRS.Events;


/// <summary>
/// Interface para handlers de Domain Events
/// Herda de INotificationHandler para compatibilidade com o Mediator
/// Permite que múltiplos handlers processem o mesmo Domain Event
/// </summary>
/// <typeparam name="TDomainEvent">Tipo do Domain Event</typeparam>
public interface IDomainEventHandler<in TDomainEvent> : INotificationHandler<TDomainEvent>
    where TDomainEvent : IDomainEvent
{
}