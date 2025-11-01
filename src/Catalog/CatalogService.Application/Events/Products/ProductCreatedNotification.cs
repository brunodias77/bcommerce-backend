using BuildingBlocks.CQRS.Events;

namespace CatalogService.Application.Events.Products;

public record ProductCreatedNotification(
    Guid ProductId,
    string Name,
    decimal Price) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}