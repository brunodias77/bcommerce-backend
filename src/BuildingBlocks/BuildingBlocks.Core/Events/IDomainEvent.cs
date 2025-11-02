namespace BuildingBlocks.Core.Events;

/// <summary>
/// Interface base para Domain Events
/// Domain Events representam algo que aconteceu no domínio
/// </summary>
public interface IDomainEvent
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