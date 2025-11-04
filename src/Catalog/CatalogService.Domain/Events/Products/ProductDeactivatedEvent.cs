using BuildingBlocks.Core.Events;

namespace CatalogService.Domain.Events.Products;

public record ProductDeactivatedEvent(
    Guid ProductId,
    string Name,
    DateTime DeactivatedAt) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}