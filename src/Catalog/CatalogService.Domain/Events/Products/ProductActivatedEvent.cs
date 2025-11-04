using BuildingBlocks.Core.Events;

namespace CatalogService.Domain.Events.Products;

public record ProductActivatedEvent(
    Guid ProductId,
    string Name,
    DateTime ActivatedAt) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}