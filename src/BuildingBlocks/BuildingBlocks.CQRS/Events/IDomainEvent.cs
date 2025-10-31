using BuildingBlocks.CQRS.Mediator;

namespace BuildingBlocks.CQRS.Events;

/// <summary>
/// Interface base para Domain Events
/// Herda de INotification para compatibilidade com o Mediator
/// Domain Events representam algo que aconteceu no domínio
/// </summary>
public interface IDomainEvent : INotification
{
    /// <summary>
    /// Identificador único do evento
    /// </summary>
    Guid Id { get; }
    
    /// <summary>
    /// Data e hora em que o evento ocorreu
    /// </summary>
    DateTime OccurredOn { get; }
}