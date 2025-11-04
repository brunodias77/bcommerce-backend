using BuildingBlocks.Core.Events;

namespace CatalogService.Domain.Events.Products;

public record ProductUnfeaturedEvent(
    Guid ProductId,
    string Name,
    DateTime UnfeaturedAt) : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}