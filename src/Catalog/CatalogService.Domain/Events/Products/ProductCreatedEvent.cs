using BuildingBlocks.CQRS.Events;

namespace CatalogService.Domain.Events.Products;

public record ProductCreatedEvent(
    Guid ProductId,
    string Name,
    decimal Price) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}